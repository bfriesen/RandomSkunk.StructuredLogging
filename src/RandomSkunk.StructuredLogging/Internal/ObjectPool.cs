using System.Collections.Concurrent;

namespace RandomSkunk.StructuredLogging;

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
    private readonly ConcurrentBag<T> _items = new();

    /// <summary>
    /// Rents an item from the pool, or creates a new one if the pool is empty.
    /// </summary>
    public T Rent() => _items.TryTake(out var item) ? item : factory();

    /// <summary>
    /// Resets <paramref name="item"/> and, if <c>shouldPool</c> approves of it, returns it to the pool -
    /// otherwise it's simply dropped, to be garbage collected normally.
    /// </summary>
    public void Return(T item)
    {
        reset(item);

        if (shouldPool(item))
            _items.Add(item);
    }
}
