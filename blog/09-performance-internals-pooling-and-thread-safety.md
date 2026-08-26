# Performance Internals: Pooling, Thread Safety, and the Cost of an Operation

The last three posts covered operation logging's API and how to use it. This one is for readers who
want to know what actually happens underneath — where the allocations go, why the feature is
opt-in for thread safety instead of always-on, and what it costs to run a `BeginOperation`/
`BeginSubOperation` tree thousands of times a second.

## The shared state behind every operation

A root operation and every sub-operation nested under it share exactly one `OperationLogState`
instance — there's no per-level object graph, just one mutable bag of state (`Logger`, `Level`,
`EventId`, `StartTime`, a `Stopwatch`, the journal `StringBuilder`, and the properties list) that
every `IOperationLog` in the tree reads from and writes into. That's why `AddProperty` called on a
sub-operation three levels deep shows up in the same final entry as one called on the root — they're
the same list.

## Pooling the journal `StringBuilder`

A busy service might begin many operations per second. Without pooling, each one would allocate
(and, almost immediately, discard) its own journal `StringBuilder`. `OperationLogPools.Journals` is
a small pool built on `ObjectPool<T>` — rent a `StringBuilder` when an operation begins, return it
when the operation is disposed and its content has already been flushed as the log message.

The interesting part is the retention policy: a returned `StringBuilder` is only kept in the pool
if its `Capacity` is `<= 4096`. An operation whose journal grew past that threshold was unusually
long-running or chatty — retaining a giant backing array for the *next* operation to rent would
waste memory on the common case, since most operations produce a much shorter journal. Let that one
be collected normally instead of pooled; the pool stays sized for the typical case, not the outlier.

The same reasoning applies to `OperationLogPools.JsonWriters`, used by `AppendJson` — a
`PooledJsonWriter` wrapping a pooled `Utf8JsonWriter`/`ArrayBufferWriter<byte>` pair, retained only
if its `Capacity` is `<= 16384`.

## How big can the pool get?

`ObjectPool<T>`'s size is capped independently of either retention policy: `MaxSize` is
`Environment.ProcessorCount * 4`, checked *before* the pricier `shouldPool` predicate runs on
`Return`. The reasoning is that the number of concurrently in-flight operations is naturally bounded
by available threads — capping pool size relative to `ProcessorCount` keeps roughly a few spare
instances per thread available without letting the pool grow unbounded under a burst of concurrent
operations. Once the pool is at capacity, `Return` simply drops the item (whatever `shouldPool`
would have said) and lets it be collected.

`state.Properties` — the `AddProperty` list — is deliberately *not* pooled. It's a plain
`List<KeyValuePair<string, object?>>?`, left `null` until the first `AddProperty` call. Most
operations either add a handful of properties or none at all, so there's little to gain from
pooling a list that's usually small or entirely absent — and leaving it `null` when unused means an
operation that never calls `AddProperty` allocates nothing for it at all.

## `AppendJson` without a materialized JSON string

`AppendJson<T>` needs to render `value` as indented JSON into the journal, but it never produces a
standalone JSON `string` to do that. Serialization writes UTF-8 bytes into a pooled
`Utf8JsonWriter`/`ArrayBufferWriter<byte>` pair, and those bytes are then transcoded directly into
the target `StringBuilder` using `ArrayPool<char>`-rented buffers — bytes to chars, straight into
the journal, with no intermediate `string` allocated and immediately discarded. The `PooledJsonWriter`
that does this work is itself pooled, per the retention policy above, so repeated `AppendJson` calls
across many operations reuse the same handful of writer instances rather than constructing a fresh
`Utf8JsonWriter` every time.

## Not thread-safe by default — and why that's the right default

Operation logging's state (the journal, the properties list, the level) is mutated directly, with no
locking, by default. That's a deliberate performance choice: the overwhelming majority of operations
are used from a single logical flow of execution — one request handler, one background job
iteration — where nothing is ever accessed concurrently, and paying for a `lock` on every `Append`/
`AddProperty` call would be pure overhead for that common case.

When an operation's sub-operations genuinely do run concurrently — the canonical case is
sub-operations launched via `Task.WhenAll` — pass `threadSafe: true` to `BeginOperation`:

```csharp
using IOperationLog log = logger.BeginOperation("ProcessBatch", threadSafe: true);

await Task.WhenAll(items.Select(async item =>
{
    using IOperationLog itemLog = log.BeginSubOperation($"Item {item.Id}");
    await ProcessAsync(item);
}));
```

This wraps the returned `IOperationLog` in a `SynchronizedOperationLog` decorator, and every member
call — `AddProperty`, `Append`, `AppendValue`, `AppendJson`, `SetResult`, `SetException`, `Escalate`,
`BeginSubOperation`, `Dispose` — takes a `lock` on a shared `gate` object before delegating to the
inner, unsynchronized implementation. `BeginSubOperation` wraps the resulting sub-operation in
*another* `SynchronizedOperationLog`, passing along the same `gate` — so the whole operation tree,
root and every nested sub-operation at any depth, ultimately synchronizes on one shared lock, not one
lock per level. That matches the single `OperationLogState` they're all protecting: one shared
mutable state, one lock guarding it.

One consequence worth knowing: under `threadSafe: true`, reading `Properties` returns a
point-in-time snapshot (`[.. inner.Properties]`, a fresh copy) rather than a live view onto the
underlying list. That's intentional — enumerating a live list while another thread might be mid-way
through an `AddProperty` call on it would be an ordinary concurrent-collection-mutation hazard.
Without `threadSafe: true`, `Properties` returns the live list directly, since there's nothing else
that could be mutating it concurrently by construction.

## What this adds up to

For the common, single-threaded case, an operation costs: one pooled `StringBuilder` rental, one
`Stopwatch` (which itself is cheap — just a `long` tick count under the hood), and whatever the
journal actually accumulates. No locking, no JSON string round-trip for `AppendJson`, and the pools
themselves stay bounded regardless of load. The `threadSafe: true` path costs a `lock` per call,
which is exactly the price you'd expect to pay for using the feature the way it demands — not a
price paid by every caller regardless of whether they need it.

## Next up

Post 10 closes out the series: a recap, where to send feedback, and a look at what might come next
for the library.
