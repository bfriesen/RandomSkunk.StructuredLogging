using FluentAssertions;
using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogStateTests
{
    [Fact]
    public void Count_ReturnsSumOfNameValuePairsLengthAndInterpolationNameValuePairsCount()
    {
        TwoItemNameValuePairArray nameValuePairs = new();
        NameValuePairList4 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, nameValuePairs);

        logState.Count.Should().Be(nameValuePairs.Length + interpolationNameValuePairs.Count);
    }

    [Fact]
    public void Indexer_RetrievesItemFromAppropriateCollection()
    {
        TwoItemNameValuePairArray nameValuePairs = new();
        NameValuePairList4 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, nameValuePairs);

        logState[0].Should().Be(nameValuePairs[0]);
        logState[1].Should().Be(nameValuePairs[1]);
        logState[2].Should().Be(interpolationNameValuePairs[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Test log message")]
    public void ToString_ReturnsMessage(string? message)
    {
        NameValuePairList4 interpolationNameValuePairs = new();
        MessageData messageData = new(message, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, new TwoItemNameValuePairArray());
        logState.ToString().Should().BeSameAs(message);
    }

    [Fact]
    public void GetEnumerator_ReturnsEnumeratorThatIteratesOverItems()
    {
        TwoItemNameValuePairArray nameValuePairs = new();
        NameValuePairList4 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, nameValuePairs);

        LogState<TwoItemNameValuePairArray>.Enumerator enumerator = logState.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(nameValuePairs[0]);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(nameValuePairs[1]);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(interpolationNameValuePairs[0]);

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void IEnumerableOfNameValuePairOfStringToObject_GetEnumerator_ReturnsEnumerator()
    {
        TwoItemNameValuePairArray nameValuePairs = new();
        NameValuePairList4 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, nameValuePairs);

        IEnumerator<KeyValuePair<string, object?>> enumerator = ((IEnumerable<KeyValuePair<string, object?>>)logState).GetEnumerator();

        enumerator.Should().BeOfType<LogState<TwoItemNameValuePairArray>.Enumerator>();
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ReturnsEnumerator()
    {
        TwoItemNameValuePairArray nameValuePairs = new();
        NameValuePairList4 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairArray> logState = new(in messageData, nameValuePairs);

        IEnumerator enumerator = ((IEnumerable)logState).GetEnumerator();
        enumerator.Should().BeOfType<LogState<TwoItemNameValuePairArray>.Enumerator>();
    }

    private readonly struct TwoItemNameValuePairArray : INameValuePairArray
    {
        public int Length => 2;

        public KeyValuePair<string, object?> this[int index] =>
            index switch
            {
                0 => new KeyValuePair<string, object?>("Foo", "abc"),
                1 => new KeyValuePair<string, object?>("Bar", 123),
                _ => throw new IndexOutOfRangeException()
            };
    }
}
