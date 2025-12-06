using FluentAssertions;
using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogStateTests
{
    [Fact]
    public void Count_ReturnsIndexableKeyValuePairsCount()
    {
        TestKeyValuePairArray keyValuePairs = new();
        LogState<TestKeyValuePairArray> logState = new(null, keyValuePairs);

        logState.Count.Should().Be(keyValuePairs.Length);
    }

    [Fact]
    public void Indexer_ReturnsIndexableKeyValuePairsIndexer()
    {
        TestKeyValuePairArray keyValuePairs = new();
        LogState<TestKeyValuePairArray> logState = new(null, keyValuePairs);

        for (int i = 0; i < keyValuePairs.Length; i++)
        {
            KeyValuePair<string, object?> expected = keyValuePairs[i];
            KeyValuePair<string, object?> actual = logState[i];
            actual.Should().Be(expected);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Test log message")]
    public void ToString_ReturnsMessage(string? message)
    {
        LogState<TestKeyValuePairArray> logState = new(message, new TestKeyValuePairArray());
        logState.ToString().Should().BeSameAs(message);
    }

    [Fact]
    public void GetEnumerator_IteratesOverIndexableKeyValuePairs()
    {
        TestKeyValuePairArray keyValuePairs = new();
        LogState<TestKeyValuePairArray> logState = new(null, keyValuePairs);

        LogState<TestKeyValuePairArray>.Enumerator enumerator = logState.GetEnumerator();

        for (int i = 0; i < keyValuePairs.Length; i++)
        {
            enumerator.MoveNext().Should().BeTrue();
            KeyValuePair<string, object?> expected = keyValuePairs[i];
            KeyValuePair<string, object?> actual = enumerator.Current;
            actual.Should().Be(expected);
        }

        enumerator.MoveNext().Should().BeFalse();
    }

    [Fact]
    public void IEnumerableOfKeyValuePairOfStringToObject_GetEnumerator_ReturnsEnumerator()
    {
        TestKeyValuePairArray keyValuePairs = new();
        LogState<TestKeyValuePairArray> logState = new(null, keyValuePairs);

        IEnumerator<KeyValuePair<string, object?>> enumerator = ((IEnumerable<KeyValuePair<string, object?>>)logState).GetEnumerator();

        enumerator.Should().BeOfType<LogState<TestKeyValuePairArray>.Enumerator>();
    }

    [Fact]
    public void IEnumerable_GetEnumerator_ReturnsEnumerator()
    {
        TestKeyValuePairArray keyValuePairs = new();
        LogState<TestKeyValuePairArray> logState = new(null, keyValuePairs);

        IEnumerator enumerator = ((IEnumerable)logState).GetEnumerator();
        enumerator.Should().BeOfType<LogState<TestKeyValuePairArray>.Enumerator>();
    }

    private readonly struct TestKeyValuePairArray : IKeyValuePairArray
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
