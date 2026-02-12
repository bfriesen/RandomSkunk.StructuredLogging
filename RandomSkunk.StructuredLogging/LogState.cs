using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines the state for a structured log entry, including the message and associated name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogState<TNameValuePairArray>(ref readonly MessageData messageData, TNameValuePairArray nameValuePairs)
    : IReadOnlyList<KeyValuePair<string, object?>>
    where TNameValuePairArray : struct, INameValuePairArray
{
    internal static readonly Func<LogState<TNameValuePairArray>, Exception?, string> Formatter =
        (state, exception) => state._message ?? string.Empty;

    private readonly string? _message = messageData.Message;
    private readonly TNameValuePairArray _nameValuePairs = nameValuePairs;
    private readonly NameValuePairList4 _interpolationNameValuePairs = messageData.InterpolationNameValuePairs;

    public int Count => _nameValuePairs.Length + _interpolationNameValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] => GetItem(_nameValuePairs, _interpolationNameValuePairs, index);

    /// <summary>Returns the log message.</summary>
    public override string? ToString() => _message;

    public Enumerator GetEnumerator() => new(in this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KeyValuePair<string, object?> GetItem(
        TNameValuePairArray nameValuePairs,
        NameValuePairList4 interpolationNameValuePairs,
        int index) =>
        index < nameValuePairs.Length
            ? nameValuePairs[index]
            : interpolationNameValuePairs[index - nameValuePairs.Length];

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator(ref readonly LogState<TNameValuePairArray> logState) : IEnumerator<KeyValuePair<string, object?>>
    {
        private TNameValuePairArray _nameValuePairs = logState._nameValuePairs;
        private NameValuePairList4 _interpolationNameValuePairs = logState._interpolationNameValuePairs;
        private int _index = -1;

        public readonly KeyValuePair<string, object?> Current => GetItem(_nameValuePairs, _interpolationNameValuePairs, _index);

        readonly object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _nameValuePairs.Length + _interpolationNameValuePairs.Count;

        public void Dispose() => this = default;

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
