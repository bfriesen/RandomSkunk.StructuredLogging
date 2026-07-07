using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Logger state used by the message-only, <c>params</c> tuple array, and
/// <see cref="IReadOnlyCollection{T}"/> structured-property overloads. Combines the properties
/// explicitly passed by the caller with any the message's interpolated string handler captured
/// via &lt;PropertyName&gt; format tags.
/// </summary>
internal readonly struct LogPropertiesState : IReadOnlyList<KeyValuePair<string, object?>>
{
    public static readonly Func<LogPropertiesState, Exception?, string> Formatter = static (state, _) => state._message;

    private readonly string _message;
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _capturedProperties;
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _explicitProperties;

    public LogPropertiesState(
        string message,
        IReadOnlyList<KeyValuePair<string, object?>> capturedProperties,
        IReadOnlyList<KeyValuePair<string, object?>> explicitProperties)
    {
        _message = message;
        _capturedProperties = capturedProperties;
        _explicitProperties = explicitProperties;
    }

    public int Count => _capturedProperties.Count + _explicitProperties.Count;

    public KeyValuePair<string, object?> this[int index] => index < _capturedProperties.Count
        ? _capturedProperties[index]
        : _explicitProperties[index - _capturedProperties.Count];

    public override string ToString() => _message;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
