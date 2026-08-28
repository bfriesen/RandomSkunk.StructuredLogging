using System.Collections.Concurrent;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A minimal thread-safe object pool. A busy application may begin many operations per second, and
/// without pooling every one of them would allocate (and immediately discard) its own
/// <see cref="System.Text.StringBuilder"/> journal and property list - see
/// <see cref="OperationLogPools"/>, the only consumer of this type.
/// </summary>
/// <typeparam name="T">The type of object to pool.</typeparam>
/// <param name="factory">Creates a new item when the pool is empty.</param>
/// <param name="reset">Clears an item's state before it's returned to the pool.</param>
/// <param name="shouldPool">
/// Decides, given an already-<paramref name="reset"/> item, whether it's still worth retaining (e.g. its
/// backing capacity hasn't grown unreasonably large) rather than letting it be garbage collected.
/// </param>
internal sealed class ObjectPool<T>(Func<T> factory, Action<T> reset, Func<T, bool> shouldPool) where T : class
{
    // Concurrently in-flight operations is naturally bounded by available threads, so scaling the
    // pool's capacity off ProcessorCount keeps roughly a few spare instances per thread without letting
    // it grow unbounded under heavy concurrency.
    private static readonly int MaxSize = Environment.ProcessorCount * 4;

    private readonly ConcurrentBag<T> _items = [];

    // Tracks _items.Count without ever reading ConcurrentBag<T>.Count, which freezes the bag - it takes
    // the global lock plus every per-thread queue lock - to produce an exact answer. That's around 50ns
    // per call and doesn't scale at all: reading it from every Return serializes the whole pool, which is
    // precisely the wrong behavior for the threadSafe path, where the Journals pool sees a rent/return
    // per Append rather than one per operation. This counter is kept in lockstep with _items instead:
    // incremented before an Add and decremented after a successful TryTake, so the only skew a racing
    // thread can observe is the pool looking momentarily fuller than it is - which at worst drops an item
    // that could have been pooled. MaxSize is a heuristic, so that's a fine trade for an uncontended read.
    private int _count;

    /// <summary>
    /// Rents an item from the pool, or creates a new one if the pool is empty.
    /// </summary>
    public T Rent()
    {
        if (!_items.TryTake(out T? item))
            return factory();

        Interlocked.Decrement(ref _count);
        return item;
    }

    /// <summary>
    /// Resets <paramref name="item"/> and, if the pool isn't already at capacity and <c>shouldPool</c>
    /// approves of it, returns it to the pool - otherwise it's simply dropped, to be garbage collected
    /// normally.
    /// </summary>
    public void Return(T item)
    {
        reset(item);

        if (Volatile.Read(ref _count) >= MaxSize || !shouldPool(item))
            return;

        Interlocked.Increment(ref _count);
        _items.Add(item);
    }
}
