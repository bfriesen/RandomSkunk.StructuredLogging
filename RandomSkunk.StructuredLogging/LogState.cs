
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines the state for a structured log entry, including the message and associated name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogState<TNameValuePairList>(ref readonly MessageData messageData, TNameValuePairList nameValuePairs)
    : IReadOnlyList<KeyValuePair<string, object?>>
    where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
{
    internal static readonly Func<LogState<TNameValuePairList>, Exception?, string> Formatter =
        (state, exception) => state._message ?? string.Empty;

    private readonly string? _message = messageData.Message;
    private readonly TNameValuePairList _nameValuePairs = nameValuePairs;
    private readonly NameValuePairList2 _additionalNameValuePairs = messageData.AdditionalNameValuePairs;

    public int Count => _nameValuePairs.Count + _additionalNameValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] => GetItem(_nameValuePairs, _additionalNameValuePairs, index);

    /// <summary>Returns the log message.</summary>
    public override string? ToString() => _message;

    public Enumerator GetEnumerator() => new(in this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static KeyValuePair<string, object?> GetItem(
        TNameValuePairList nameValuePairs,
        NameValuePairList2 interpolationNameValuePairs,
        int index) =>
        index < nameValuePairs.Count
            ? nameValuePairs[index]
            : interpolationNameValuePairs[index - nameValuePairs.Count];

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator(ref readonly LogState<TNameValuePairList> logState) : IEnumerator<KeyValuePair<string, object?>>
    {
        private TNameValuePairList _nameValuePairs = logState._nameValuePairs;
        private NameValuePairList2 _additionalLogProperties = logState._additionalNameValuePairs;
        private int _index = -1;

        public readonly KeyValuePair<string, object?> Current => GetItem(_nameValuePairs, _additionalLogProperties, _index);

        readonly object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _nameValuePairs.Count + _additionalLogProperties.Count;

        public void Dispose() => this = default;

        void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
