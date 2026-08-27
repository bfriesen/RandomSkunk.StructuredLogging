# From Scattered Log Lines to One Wide Event: A Refactoring Case Study

Posts 6 and 7 covered operation logging's API in isolation — small, tidy examples. This post walks
through a full refactor: a realistic method with the conventional scattered-log-line style, and the
same method converted end-to-end to operation logging.

## The starting point

An order-fulfillment method: validate, allocate inventory, charge payment, ship. Logged the
conventional way:

```csharp
public async Task<OrderResult> FulfillAsync(Order order)
{
    _logger.LogInformation("Validating order {OrderId}", order.Id);
    if (!IsValid(order, out string reason))
    {
        _logger.LogWarning("Order {OrderId} rejected: {Reason}", order.Id, reason);
        return OrderResult.Rejected(reason);
    }

    _logger.LogInformation("Allocating inventory for order {OrderId}", order.Id);
    AllocationResult allocation;
    try
    {
        allocation = await _inventory.AllocateAsync(order.Items);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Inventory allocation failed for order {OrderId}", order.Id);
        throw;
    }
    _logger.LogInformation("Allocation result for order {OrderId}: {Status}", order.Id, allocation.Status);

    _logger.LogInformation("Charging payment for order {OrderId}, amount {Amount}", order.Id, order.Total);
    PaymentResult payment = await _paymentGateway.ChargeAsync(order.PaymentMethod, order.Total);
    if (!payment.Succeeded)
    {
        _logger.LogWarning("Payment failed for order {OrderId}: {Reason}", order.Id, payment.FailureReason);
        await _inventory.ReleaseAsync(order.Items);
        return OrderResult.PaymentFailed(payment.FailureReason);
    }

    _logger.LogInformation("Creating shipment for order {OrderId}", order.Id);
    string trackingNumber = await _shipping.CreateShipmentAsync(order.Address, order.Items);
    _logger.LogInformation("Order {OrderId} shipped, tracking {TrackingNumber}", order.Id, trackingNumber);

    return OrderResult.Shipped(trackingNumber);
}
```

Nothing here is unusual — this is what a lot of production code looks like. But it has all the
problems posts 6 and 7 argued against:

- **Seven independent log lines** for one logical operation, correlated only by `OrderId` appearing
  in each template — reconstructing "what happened for order 8842" means gathering all seven from
  wherever they landed and putting them back in order by hand.
- **One exception path logs and rethrows.** If the caller also logs the exception (common, since the
  caller doesn't know this method already did), the same failure shows up twice in the log stream.
- **The rejected/backordered/payment-failed paths only ever produce one `LogWarning` line each** —
  if you need to know *why* validation failed in more detail, or what the allocation actually
  returned before the payment failed, that context was never captured because there was no low-cost
  place to put it that wasn't its own separate log line.
- **Under concurrent load, these seven lines interleave with every other in-flight order's seven
  lines.** Nothing about the log stream itself groups them.

## The refactor

```csharp
public async Task<OrderResult> FulfillAsync(Order order)
{
    using IOperationLog log = _logger.BeginOperation("FulfillOrder");
    log.AddProperty("OrderId", order.Id);

    try
    {
        if (!IsValid(order, out string reason))
        {
            log.Escalate(LogLevel.Warning);
            return new OrderResult.Rejected(reason).RecordResultTo(log);
        }

        using (IOperationLog allocLog = log.BeginSubOperation("AllocateInventory"))
        {
            try
            {
                AllocationResult allocation = await _inventory.AllocateAsync(order.Items);
                allocLog.SetResult(allocation.Status);
            }
            catch (Exception ex)
            {
                allocLog.SetException(ex);
                throw;
            }
        }

        PaymentResult payment;
        using (IOperationLog paymentLog = log.BeginSubOperation("ChargePayment"))
        {
            payment = await _paymentGateway.ChargeAsync(order.PaymentMethod, order.Total);
            paymentLog.SetResult(payment.Succeeded);

            if (!payment.Succeeded)
                paymentLog.Escalate(LogLevel.Warning);
        }

        if (!payment.Succeeded)
        {
            await _inventory.ReleaseAsync(order.Items);
            return new OrderResult.PaymentFailed(payment.FailureReason).RecordResultTo(log);
        }

        using IOperationLog shipLog = log.BeginSubOperation("CreateShipment");
        string trackingNumber = await _shipping.CreateShipmentAsync(order.Address, order.Items);
        shipLog.SetResult(trackingNumber);

        return new OrderResult.Shipped(trackingNumber).RecordResultTo(log);
    }
    catch (Exception ex)
    {
        log.SetException(ex);
        log.Escalate(LogLevel.Error);
        throw;
    }
}
```

## What changed, and why

**One `using` at the top instead of seven scattered calls.** Every code path through this method —
rejected, backordered, payment-failed, shipped, or thrown — ends with exactly one log entry,
because `log`'s disposal is the only thing that ever writes one.

**The root `try`/`catch` replaces per-call exception logging.** `log.SetException(ex)` records the
exception on the final entry once; there's no risk of the caller logging it again, because this
method no longer logs the exception at all — it only records it onto the operation and rethrows,
exactly as before.

**A nested `try`/`catch` inside `AllocateInventory` still makes sense**, but for a different
reason than in the original: not to log the exception (the root catch will record it either way),
but to attach a `` `AllocateInventory` failed: ... `` line to the journal *before* rethrowing, so
the final entry shows which step failed, not just that something failed somewhere.

**`Escalate` replaces `LogWarning`/`LogError` for non-exceptional failures.** A rejected order or a
declined payment isn't an exception — nothing throws — so there's nothing for a root `catch` to
observe. `Escalate(LogLevel.Warning)` marks the eventual entry as warning-worthy anyway, at the
exact point the code decides that's true, without needing a separate log call to say so.

**The exception path calls `Escalate(LogLevel.Error)` explicitly.** Recording an exception via
`SetException` doesn't by itself raise the entry's level (see [post 7](07-nested-operations-and-the-journal.md))
— an operation begun at `Information` stays at `Information` unless something escalates it. Calling
both together here is a deliberate choice: any uncaught exception should produce an `Error`-level
entry, not an `Information`-level one with an exception attached.

**Every returned result now flows through `RecordResultTo(log)`.** Whatever the method returns —
rejected, backordered, payment-failed, or shipped — becomes the `Operation.Result` structured
property on the final entry, and the fluent form means the `return` statement's shape doesn't
change at all.

## What the log stream looks like afterward

A rejected order now produces one entry, at `Warning`:

```
Operation: FulfillOrder
Start Time: 2026-08-19 12:56:31.417 -04:00
------------------------------------------
[0.001] Operation escalated from Information to Warning.
[0.001] Operation complete.
```

with `OrderId` and `Operation.Result` (the `Rejected(reason)` value) as structured properties — the
rejection reason is captured without ever needing its own log line, and the journal itself explains
why this entry is at `Warning` instead of the operation's usual `Information`. A successful order
produces one
entry, at `Information`, whose message shows the full three-step timeline exactly the way post 7's
example did. Either way, there's exactly one thing to find per order, not up to seven, and nothing
about the log stream depends on interleaving working out in your favor.

## A checklist for your own refactors

- Replace the method's `LogInformation`/`LogDebug` step-narration calls with `Append`/`AppendValue`/
  `AppendJson` on the operation (or a sub-operation, for a distinct step worth its own name).
- Replace a `LogWarning`/`LogError` that doesn't correspond to a thrown exception with `Escalate`.
- Replace a `catch { LogError(...); throw; }` with `SetException` (plus `Escalate` if the level
  should change) — keep the `catch` if you want a sub-operation-specific journal line before
  rethrowing, drop it if the root's `catch` already covers what you need.
- Route every return value through `RecordResultTo`/`SetResult` instead of a final "done" log line.
- Don't forget the `using` — remember `RSSL0006` (from [post 5](05-migrating-with-analyzers.md))
  exists specifically to flag a `BeginOperation`/`BeginSubOperation` call whose result never gets
  disposed, since that silently drops the entire journal.

## Next up

Post 9 goes under the hood: the pooling and thread-safety machinery that keeps operation logging
cheap even for large, deeply nested operations.
