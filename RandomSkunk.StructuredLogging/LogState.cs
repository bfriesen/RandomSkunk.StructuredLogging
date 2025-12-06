using System.Collections;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogState<TKeyValuePairArray>(string? message, TKeyValuePairArray keyValuePairs)
    : IReadOnlyList<KeyValuePair<string, object?>>
    where TKeyValuePairArray : struct, IKeyValuePairArray
{
    internal static readonly Func<LogState<TKeyValuePairArray>, Exception?, string> Formatter =
        (state, exception) => state.ToString() ?? string.Empty;

    public int Count => keyValuePairs.Length;

    public KeyValuePair<string, object?> this[int index] => keyValuePairs[index];

    internal TKeyValuePairArray KeyValuePairs => keyValuePairs;

    /// <summary>Returns the log message.</summary>
    public override string? ToString() => message;

    public Enumerator GetEnumerator() => new(in keyValuePairs);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator(ref readonly TKeyValuePairArray keyValuePairs) : IEnumerator<KeyValuePair<string, object?>>
    {
        private TKeyValuePairArray _keyValuePairs = keyValuePairs;
        private int _index = -1;

        public readonly KeyValuePair<string, object?> Current => _keyValuePairs[_index];

        readonly object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _keyValuePairs.Length;

        public void Dispose() => this = default;

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
