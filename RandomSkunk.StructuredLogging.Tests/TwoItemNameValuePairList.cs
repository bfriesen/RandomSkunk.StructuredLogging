using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

public readonly struct TwoItemNameValuePairList : IReadOnlyList<KeyValuePair<string, object?>>
{
    public int Count => 2;

    public KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new KeyValuePair<string, object?>("Foo", "abc"),
            1 => new KeyValuePair<string, object?>("Bar", 123),
            _ => throw new IndexOutOfRangeException()
        };

    public NameValuePairListEnumerator<TwoItemNameValuePairList> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
