using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines the state for a structured log entry, including the message and associated key-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogState<TKeyValuePairArray>(ref readonly MessageData messageData, TKeyValuePairArray keyValuePairs)
    : IReadOnlyList<KeyValuePair<string, object?>>
    where TKeyValuePairArray : struct, IKeyValuePairArray
{
    internal static readonly Func<LogState<TKeyValuePairArray>, Exception?, string> Formatter =
        (state, exception) => state._message ?? string.Empty;

    private readonly string? _message = messageData.Message;
    private readonly TKeyValuePairArray _keyValuePairs = keyValuePairs;
    private readonly KeyValuePairList4 _interpolationKeyValuePairs = messageData.InterpolationKeyValuePairs;

    public int Count => _keyValuePairs.Length + _interpolationKeyValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] => GetItem(_keyValuePairs, _interpolationKeyValuePairs, index);

    /// <summary>Returns the log message.</summary>
    public override string? ToString() => _message;

    public Enumerator GetEnumerator() => new(in this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KeyValuePair<string, object?> GetItem(
        TKeyValuePairArray keyValuePairs,
        KeyValuePairList4 interpolationKeyValuePairs,
        int index) =>
        index < keyValuePairs.Length
            ? keyValuePairs[index]
            : interpolationKeyValuePairs[index - keyValuePairs.Length];

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator(ref readonly LogState<TKeyValuePairArray> logState) : IEnumerator<KeyValuePair<string, object?>>
    {
        private TKeyValuePairArray _keyValuePairs = logState._keyValuePairs;
        private KeyValuePairList4 _interpolationKeyValuePairs = logState._interpolationKeyValuePairs;
        private int _index = -1;

        public readonly KeyValuePair<string, object?> Current => GetItem(_keyValuePairs, _interpolationKeyValuePairs, _index);

        readonly object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _keyValuePairs.Length + _interpolationKeyValuePairs.Count;

        public void Dispose() => this = default;

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
