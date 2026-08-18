using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Concatenates two <see cref="IReadOnlyList{T}"/> instances of <see cref="KeyValuePair{TKey, TValue}"/>
/// without copying either one. Used to combine a message's tag-captured properties with a caller-supplied
/// <see cref="IReadOnlyCollection{T}"/> of properties.
/// </summary>
internal readonly struct ConcatPropertyList(
    IReadOnlyList<KeyValuePair<string, object?>> first,
    IReadOnlyList<KeyValuePair<string, object?>> second) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public int Count => first.Count + second.Count;

    public KeyValuePair<string, object?> this[int index] => index < first.Count
        ? first[index]
        : second[index - first.Count];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
