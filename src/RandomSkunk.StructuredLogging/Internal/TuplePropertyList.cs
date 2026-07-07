using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Adapts a <c>(string Name, object? Value)[]</c> array to <see cref="IReadOnlyList{T}"/> of
/// <see cref="KeyValuePair{TKey, TValue}"/> without copying the array.
/// </summary>
internal readonly struct TuplePropertyList : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly (string Name, object? Value)[] _properties;

    public TuplePropertyList((string Name, object? Value)[] properties) => _properties = properties;

    public int Count => _properties.Length;

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            var (name, value) = _properties[index];
            return new KeyValuePair<string, object?>(name, value);
        }
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        foreach (var (name, value) in _properties)
            yield return new KeyValuePair<string, object?>(name, value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
