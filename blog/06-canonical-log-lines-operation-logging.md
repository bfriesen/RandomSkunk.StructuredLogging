# Canonical Log Lines in .NET: Introducing Operation Logging

Everything so far in this series has been about a single call site: one message, some properties,
one line written to your logging provider. This post is about a different problem — one that shows
up once an operation involves more than one step.

## The problem with one log line per step

Take a fairly ordinary order-fulfillment flow. It validates the order, charges a payment, and
updates inventory. Logged the conventional way, that's several independent log lines:

```csharp
logger.Information($"Validating order {orderId}");
logger.Information($"Charging payment for order {orderId}");
logger.Information($"Payment succeeded: {receipt}");
logger.Information($"Order {orderId} fulfilled in {elapsed}ms");
```

Each line is reasonable on its own. But once this code runs in production, next to a hundred other
requests logging the same way, concurrently, those four lines land in the log stream interleaved
with everyone else's four lines. Answering "what happened during *this* request" means filtering by
some correlation ID, gathering every line that shares it, and mentally reconstructing the
sequence — across whatever log viewer you're using, every single time you debug something. And if
one step's line goes missing (a level filter, a dropped log line under load, a crash before that
statement runs), you're reconstructing an incomplete picture without knowing it's incomplete.

## Canonical log lines: one entry per operation

This idea has a name outside of .NET: **canonical log lines** (sometimes called "wide events"). The
premise is simple — instead of writing many narrow log lines over the course of a request or
operation, accumulate everything that happens into one wide record, and emit it exactly once, when
the operation finishes. One entry per operation means one thing to find, one thing to filter on,
and no reconstruction step at read time. If the operation doesn't crash before completion, its
entire story — including everything that happened along the way — is guaranteed to be in that one
place.

RandomSkunk.StructuredLogging implements this as **operation logging**, a second feature living
alongside the per-call logging methods covered in the earlier posts.

## What it looks like

```csharp
using RandomSkunk.StructuredLogging;
using RandomSkunk.StructuredLogging.Operation;

using var log = logger.BeginOperation("FulfillOrder");

log.AddProperty("OrderId", orderId);
log.Append($"Validating order {orderId}");

using (var paymentLog = log.BeginSubOperation("ChargePayment"))
{
    try
    {
        Receipt receipt = await paymentGateway.ChargeAsync(orderId);
        paymentLog.SetResult(receipt);
    }
    catch (Exception ex)
    {
        paymentLog.SetException(ex);
        throw;
    }
}

return order.RecordResultTo(log);
```

`BeginOperation` returns an `IOperationLog`. Nothing is written to your logging provider yet —
everything from here is accumulating into an in-memory journal. `AddProperty` attaches a structured
property to the eventual entry. `Append` writes a free-text line to the journal. `BeginSubOperation`
starts a nested sub-operation — its own journal lines get appended to the *same* journal, prefixed
with its own name, but a sub-operation never writes a log entry of its own. Only disposing the root
`log` actually writes anything.

When that happens, the whole accumulated journal becomes the message of exactly one log entry:

```
Operation: FulfillOrder
Start Time: 2026-08-19 12:56:31.417 -04:00
------------------------------------------
[0.002] Validating order 42
[0.003] `ChargePayment` started.
[0.041] `ChargePayment` result: Receipt { Id = ..., Amount = 99.00 }
[0.041] `ChargePayment` complete.
[0.042] Operation complete.
```

Every line is timestamped with elapsed seconds since the operation began, so the sequence and
timing of every step are visible in one place, in order, without correlating anything across
separate log lines. Alongside that message, the entry's structured properties include
`Operation.Name`, `Operation.StartTime`, `Operation.DurationSeconds`, `Operation.Result` (set here
via the fluent `RecordResultTo` at the end), and every property added via `AddProperty` —
`OrderId`, in this example.

## It costs nothing when the level is disabled

Same principle as the per-call methods from earlier posts: `BeginOperation` checks
`logger.IsEnabled(level)` up front. If the level is disabled, it returns an `IOperationLog` that
never accumulates a journal and never writes anything — `Append`/`AddProperty`/`BeginSubOperation`/
etc. all become no-ops. You don't need to guard the whole block with an `IsEnabled` check yourself;
the cost of an operation you're not logging is close to nothing.

`IOperationLog` exposes that same answer back to you, as `log.IsEnabled` — not to guard the whole
block (you still don't need that), but for the rarer case where building a value to hand to
`AddProperty`/`AppendValue`/`AppendJson` is itself expensive enough to be worth skipping:

```csharp
if (log.IsEnabled)
    log.AppendJson(BuildExpensiveDiagnosticSnapshot());
```

## This is additive, not a replacement

Operation logging doesn't replace the `Information`/`Debug`/etc. extension methods from earlier
posts — it sits alongside them for a different situation. A single, self-contained event (a request
completing, a background job step) is still often best as one ordinary log call. Operation logging
earns its keep once an operation has *internal structure worth preserving* — sub-steps, sub-results,
a result or exception that belongs to the whole operation rather than any one step within it — and
you'd otherwise be scattering that structure across several independent lines.

## Next up

Post 7 goes deeper into nested sub-operations: how root and sub-operation behavior differs for
`SetResult`/`SetException`, what `Escalate` does, and what the emitted journal and structured
properties look like in more complex, multi-level operations.
