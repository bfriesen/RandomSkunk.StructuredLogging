using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Logger state used by the 0-arity structured-property overloads, with or without the leading
/// <see cref="IReadOnlyCollection{T}"/> parameter. Combines the properties explicitly passed by the
/// caller with any the message's interpolated string handler captured via &lt;PropertyName&gt;
/// format tags.
/// <para>
/// <paramref name="logProperties"/> is addressed before <paramref name="capturedProperties"/>,
/// matching <c>LogPropertiesState&lt;T1..T6&gt;</c> and the order the README documents: the
/// collection's entries, then tag-captured properties, then the trailing per-call properties. The
/// relative order of the collection and the captured properties must not depend on how many
/// per-call properties a call happens to pass, or adding one would silently reorder the rest.
/// </para>
/// </summary>
internal readonly struct LogPropertiesState(
    string message,
    IReadOnlyList<KeyValuePair<string, object?>> logProperties,
    IReadOnlyList<KeyValuePair<string, object?>> capturedProperties) : IReadOnlyList<KeyValuePair<string, object?>>
{
    public static readonly Func<LogPropertiesState, Exception?, string> Formatter = static (state, _) => state._message;

    private readonly string _message = message;

    public int Count => logProperties.Count + capturedProperties.Count;

    public KeyValuePair<string, object?> this[int index] => index < logProperties.Count
        ? logProperties[index]
        : capturedProperties[index - logProperties.Count];

    public override string ToString() => _message;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
