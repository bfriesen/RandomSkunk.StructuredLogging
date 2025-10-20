using System.Collections;

namespace RandomSkunk.StructuredLogging;

using LogAttribute = (string Key, object? Value);

internal readonly struct LogState<TLogAttributeArray>(string? logMessage, TLogAttributeArray logAttributes)
    : IReadOnlyList<KeyValuePair<string, object?>>
    where TLogAttributeArray : struct, ILogAttributeArray
{
    internal static readonly Func<LogState<TLogAttributeArray>, Exception?, string> Formatter = (state, exception) => state.ToString() ?? string.Empty;

    public int Count => logAttributes.Size;

    public KeyValuePair<string, object?> this[int index]
    {
        get
        {
            LogAttribute logAttribute = logAttributes.Get(index);
            return new KeyValuePair<string, object?>(logAttribute.Key, logAttribute.Value);
        }
    }

    /// <summary>Returns the log message.</summary>
    public override string? ToString() => logMessage;

    public Enumerator GetEnumerator() => new(logAttributes);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(TLogAttributeArray logAttributes)
        : IEnumerator<KeyValuePair<string, object?>>
    {
        private int _index = -1;

        public readonly KeyValuePair<string, object?> Current
        {
            get
            {
                LogAttribute logAttribute = logAttributes.Get(_index);
                return new KeyValuePair<string, object?>(logAttribute.Key, logAttribute.Value);
            }
        }

        readonly object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < logAttributes.Size;

        readonly void IDisposable.Dispose() { }

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
