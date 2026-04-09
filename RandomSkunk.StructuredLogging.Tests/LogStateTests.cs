using FluentAssertions;
using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogStateTests
{
    [Fact]
    public void Count_ReturnsSumOfNameValuePairsLengthAndInterpolationNameValuePairsCount()
    {
        TwoItemNameValuePairList nameValuePairs = new();
        NameValuePairList2 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, nameValuePairs);

        logState.Count.Should().Be(nameValuePairs.Count + interpolationNameValuePairs.Count);
    }

    [Fact]
    public void Indexer_RetrievesItemFromAppropriateCollection()
    {
        TwoItemNameValuePairList nameValuePairs = new();
        NameValuePairList2 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, nameValuePairs);

        logState[0].Should().Be(nameValuePairs[0]);
        logState[1].Should().Be(nameValuePairs[1]);
        logState[2].Should().Be(interpolationNameValuePairs[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Test log message")]
    public void ToString_ReturnsMessage(string? message)
    {
        NameValuePairList2 interpolationNameValuePairs = new();
        MessageData messageData = new(message, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, new TwoItemNameValuePairList());
        logState.ToString().Should().BeSameAs(message);
    }

    [Fact]
    public void GetEnumerator_ReturnsEnumeratorThatIteratesOverItems()
    {
        TwoItemNameValuePairList nameValuePairs = new();
        NameValuePairList2 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, nameValuePairs);

        LogState<TwoItemNameValuePairList>.Enumerator enumerator = logState.GetEnumerator();

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
        TwoItemNameValuePairList nameValuePairs = new();
        NameValuePairList2 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, nameValuePairs);

        IEnumerator<KeyValuePair<string, object?>> enumerator = ((IEnumerable<KeyValuePair<string, object?>>)logState).GetEnumerator();

        enumerator.Should().BeOfType<LogState<TwoItemNameValuePairList>.Enumerator>();
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ReturnsEnumerator()
    {
        TwoItemNameValuePairList nameValuePairs = new();
        NameValuePairList2 interpolationNameValuePairs = new() { new("Baz", true) };
        MessageData messageData = new(null, in interpolationNameValuePairs);
        LogState<TwoItemNameValuePairList> logState = new(in messageData, nameValuePairs);

        IEnumerator enumerator = ((IEnumerable)logState).GetEnumerator();
        enumerator.Should().BeOfType<LogState<TwoItemNameValuePairList>.Enumerator>();
    }
}
