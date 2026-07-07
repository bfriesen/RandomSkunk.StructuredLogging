using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Adapts a <c>(string Name, object? Value)[]</c> array to <see cref="IReadOnlyList{T}"/> of
/// <see cref="KeyValuePair{TKey, TValue}"/> without copying the array.
/// </summary>
internal readonly struct TuplePropertyList((string Name, object? Value)[] properties) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public int Count => properties.Length;

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            var (name, value) = properties[index];
            return new KeyValuePair<string, object?>(name, value);
        }
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        foreach (var (name, value) in properties)
            yield return new KeyValuePair<string, object?>(name, value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
