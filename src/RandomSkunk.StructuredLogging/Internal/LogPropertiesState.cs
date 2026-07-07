using System.Collections;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Logger state used by the message-only, <c>params</c> tuple array, and
/// <see cref="IReadOnlyCollection{T}"/> structured-property overloads.
/// </summary>
internal readonly struct LogPropertiesState : IReadOnlyList<KeyValuePair<string, object?>>
{
    public static readonly Func<LogPropertiesState, Exception?, string> Formatter = static (state, _) => state._message;

    private readonly string _message;
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _properties;

    public LogPropertiesState(string message, IReadOnlyList<KeyValuePair<string, object?>> properties)
    {
        _message = message;
        _properties = properties;
    }

    public int Count => _properties.Count;

    public KeyValuePair<string, object?> this[int index] => _properties[index];

    public override string ToString() => _message;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _properties.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
