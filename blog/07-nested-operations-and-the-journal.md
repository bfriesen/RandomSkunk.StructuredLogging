# Nested Operations and the Operation Journal

[Post 6](06-canonical-log-lines-operation-logging.md) introduced operation logging with a
single-payment example: one root operation, one sub-operation, one final entry. Real operations are
rarely that flat. This post covers what changes — and, just as importantly, what *doesn't* — once
an operation has multiple sub-operations, nested several levels deep, each contributing its own
piece of the story.

## `IOperationLog` is one interface for every level

There's no separate "sub-operation" type. `BeginOperation` returns an `IOperationLog`; so does
`BeginSubOperation`, called on any existing `IOperationLog` — root or sub-operation alike:

```csharp
using var log = logger.BeginOperation("FulfillOrder");

using (var allocLog = log.BeginSubOperation("AllocateInventory"))
{
    foreach (Item item in order.Items)
    {
        using IOperationLog itemLog = allocLog.BeginSubOperation($"Reserve {item.Sku}");
        int reserved = inventory.Reserve(item.Sku, item.Quantity);
        itemLog.Append($"reserved {reserved} of {item.Quantity}");
    }
}
```

Any depth of nesting works the same way, because every level is contributing to the exact same
underlying journal and property list — there's no per-level object graph to reason about, just one
shared journal that every `IOperationLog` in the tree appends to, each sub-operation's lines
appearing at the point in elapsed time they actually happened.

## Root vs. sub-operation: same interface, different behavior

`IOperationLog`'s members split into two groups. Some behave identically no matter which level you
call them on, because they contribute to the *one* eventual entry regardless of where in the tree
they're called:

- **`AddProperty`** — every property added anywhere in the tree lands in the same structured
  property list on the final entry.
- **`EventId`** — the same value everywhere in the tree; it's the `EventId` the *final* entry gets
  written with.
- **`Escalate`** — affects the one level the eventual entry is written at, regardless of which
  level in the tree calls it (more on this below).
- **`Append`/`AppendValue`/`AppendJson`** — always write to the one shared journal.

Others are deliberately asymmetric, because only the root operation ever produces a log entry:

- **`SetResult<T>(value)`** — on the root, sets the `Operation.Result` structured property on the
  final entry. On a sub-operation, it instead appends a result line to the journal — there's no
  such thing as a sub-operation's own `Operation.Result`, since a sub-operation never gets its own
  entry.
- **`SetException(exception)`** — on the root, becomes the final log entry's `Exception`. On a
  sub-operation, it appends a "failed" line to the journal describing the exception instead.

This asymmetry isn't an inconsistency to work around — it's the whole point. A sub-operation
represents a step *within* the story, not a story of its own; its result and its exception are
details worth journaling, not top-level facts about the request.

## `Escalate`: letting a deep failure raise the whole operation's level

`BeginOperation` picks a level up front (`LogLevel.Information` by default) — and that level
decides two things at once: whether the operation logs anything at all (an `IsEnabled` check, same
as the per-call methods), and what level the eventual entry gets written at if it does log.

Sometimes you don't know until partway through the operation that it deserves a higher level. A
business failure three sub-operations deep — a declined payment, a backordered item — might not
throw an exception at all, but it's still worth a `Warning` or `Error` entry rather than an
`Information` one. That's what `Escalate` is for:

```csharp
using (IOperationLog paymentLog = log.BeginSubOperation("ChargePayment"))
{
    PaymentResult result = await paymentGateway.ChargeAsync(orderId);
    paymentLog.SetResult(result);

    if (result.Declined)
        paymentLog.Escalate(LogLevel.Warning);
}
```

A few things worth being precise about:

- **It only raises, never lowers.** `Escalate(LogLevel.Warning)` on an operation already at `Error`
  is a no-op — it only takes effect if `level` is *more severe* than the operation's current level.
- **It can't re-enable a disabled operation.** If the operation was begun at a level `IsEnabled`
  said was off, it never journaled anything in the first place, and `Escalate` on it is a no-op too
  (there's nothing to escalate). This is different from the level passed to `BeginOperation`, which
  also gates whether the operation does *any* work at all — `Escalate` can only raise the level of
  an operation that's already actively journaling.
- **It works from any level in the tree.** Calling `Escalate` on a sub-operation three levels deep
  affects the same one thing calling it on the root would — the level the single eventual entry
  gets written at — exactly the way `AddProperty` does.
- **It's independent of `SetException`.** Recording an exception doesn't automatically escalate the
  operation's level; call both together when a caught exception should also raise the severity,
  or call `Escalate` alone for a failure that never throws.

## What a deeper journal looks like

Put together, a multi-level operation like the inventory-allocation example above produces one
entry whose message shows the whole nested timeline, in the order things actually happened:

```
Operation: FulfillOrder
Start Time: 2026-08-19 12:56:31.417 -04:00
------------------------------------------
[0.001] `AllocateInventory` started.
[0.004] `Reserve SKU-100` started.
[0.006] reserved 2 of 2
[0.006] `Reserve SKU-100` complete.
[0.007] `Reserve SKU-200` started.
[0.009] reserved 0 of 1
[0.009] `Reserve SKU-200` complete.
[0.010] `AllocateInventory` complete.
[0.011] Operation complete.
```

No correlation IDs, no separate lines to gather from a log viewer — the full sequence, including
every sub-operation's own steps, is right there, in order, in the single entry the root operation
writes when it's disposed.

## Next up

Post 8 puts this all together with a full refactoring case study: taking a realistic method
littered with individual log calls and converting it end-to-end to operation logging.
