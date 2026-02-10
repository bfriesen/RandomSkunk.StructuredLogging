using FluentAssertions;
using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogStateTests
{
    [Fact]
    public void Count_ReturnsSumOfKeyValuePairsLengthAndInterpolationKeyValuePairsCount()
    {
        TwoItemKeyValuePairArray keyValuePairs = new();
        KeyValuePairList4 interpolationKeyValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, keyValuePairs);

        logState.Count.Should().Be(keyValuePairs.Length + interpolationKeyValuePairs.Count);
    }

    [Fact]
    public void Indexer_RetrievesItemFromAppropriateCollection()
    {
        TwoItemKeyValuePairArray keyValuePairs = new();
        KeyValuePairList4 interpolationKeyValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, keyValuePairs);

        logState[0].Should().Be(keyValuePairs[0]);
        logState[1].Should().Be(keyValuePairs[1]);
        logState[2].Should().Be(interpolationKeyValuePairs[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Test log message")]
    public void ToString_ReturnsMessage(string? message)
    {
        KeyValuePairList4 interpolationKeyValuePairs = new();
        MessageData messageData = new(message, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, new TwoItemKeyValuePairArray());
        logState.ToString().Should().BeSameAs(message);
    }

    [Fact]
    public void GetEnumerator_ReturnsEnumeratorThatIteratesOverItems()
    {
        TwoItemKeyValuePairArray keyValuePairs = new();
        KeyValuePairList4 interpolationKeyValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, keyValuePairs);

        LogState<TwoItemKeyValuePairArray>.Enumerator enumerator = logState.GetEnumerator();

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(keyValuePairs[0]);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(keyValuePairs[1]);

        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(interpolationKeyValuePairs[0]);

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void IEnumerableOfKeyValuePairOfStringToObject_GetEnumerator_ReturnsEnumerator()
    {
        TwoItemKeyValuePairArray keyValuePairs = new();
        KeyValuePairList4 interpolationKeyValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, keyValuePairs);

        IEnumerator<KeyValuePair<string, object?>> enumerator = ((IEnumerable<KeyValuePair<string, object?>>)logState).GetEnumerator();

        enumerator.Should().BeOfType<LogState<TwoItemKeyValuePairArray>.Enumerator>();
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ReturnsEnumerator()
    {
        TwoItemKeyValuePairArray keyValuePairs = new();
        KeyValuePairList4 interpolationKeyValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationKeyValuePairs);
        LogState<TwoItemKeyValuePairArray> logState = new(in messageData, keyValuePairs);

        IEnumerator enumerator = ((IEnumerable)logState).GetEnumerator();
        enumerator.Should().BeOfType<LogState<TwoItemKeyValuePairArray>.Enumerator>();
    }

    private readonly struct TwoItemKeyValuePairArray : IKeyValuePairArray
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
