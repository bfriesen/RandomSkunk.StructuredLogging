using FluentAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class KeyValuePairList4Tests
{
    [Fact]
    public void BehavesLikeAList()
    {
        KeyValuePair<string, object?> kvp1 = new("Key1", 1);
        KeyValuePair<string, object?> kvp2 = new("Key2", 2);
        KeyValuePair<string, object?> kvp3 = new("Key3", 3);
        KeyValuePair<string, object?> kvp4 = new("Key4", 4);
        KeyValuePair<string, object?> kvp5 = new("Key5", 5);
        KeyValuePair<string, object?> kvp6 = new("Key6", 6);

        KeyValuePairList4 keyValuePairs = default;
        keyValuePairs.Count.Should().Be(0);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[0]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp1);
        keyValuePairs.Count.Should().Be(1);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[1]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp2);
        keyValuePairs.Count.Should().Be(2);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[2]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp3);
        keyValuePairs.Count.Should().Be(3);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[3]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp4);
        keyValuePairs.Count.Should().Be(4);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[4]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp5);
        keyValuePairs.Count.Should().Be(5);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[5]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp6);
        keyValuePairs.Count.Should().Be(6);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[6]).Should().Throw<IndexOutOfRangeException>();
    }
}

public class KeyValuePairList8Tests
{
    [Fact]
    public void BehavesLikeAList()
    {
        KeyValuePair<string, object?> kvp1 = new("Key1", 1);
        KeyValuePair<string, object?> kvp2 = new("Key2", 2);
        KeyValuePair<string, object?> kvp3 = new("Key3", 3);
        KeyValuePair<string, object?> kvp4 = new("Key4", 4);
        KeyValuePair<string, object?> kvp5 = new("Key5", 5);
        KeyValuePair<string, object?> kvp6 = new("Key6", 6);
        KeyValuePair<string, object?> kvp7 = new("Key7", 7);
        KeyValuePair<string, object?> kvp8 = new("Key8", 8);
        KeyValuePair<string, object?> kvp9 = new("Key9", 9);
        KeyValuePair<string, object?> kvp10 = new("Key10", 10);

        KeyValuePairList8 keyValuePairs = default;
        keyValuePairs.Count.Should().Be(0);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[0]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp1);
        keyValuePairs.Count.Should().Be(1);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[1]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp2);
        keyValuePairs.Count.Should().Be(2);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[2]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp3);
        keyValuePairs.Count.Should().Be(3);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[3]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp4);
        keyValuePairs.Count.Should().Be(4);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[4]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp5);
        keyValuePairs.Count.Should().Be(5);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[5]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp6);
        keyValuePairs.Count.Should().Be(6);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[6]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp7);
        keyValuePairs.Count.Should().Be(7);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs[6].Should().Be(kvp7);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[7]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp8);
        keyValuePairs.Count.Should().Be(8);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs[6].Should().Be(kvp7);
        keyValuePairs[7].Should().Be(kvp8);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[8]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp9);
        keyValuePairs.Count.Should().Be(9);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs[6].Should().Be(kvp7);
        keyValuePairs[7].Should().Be(kvp8);
        keyValuePairs[8].Should().Be(kvp9);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[9]).Should().Throw<IndexOutOfRangeException>();

        keyValuePairs.Add(kvp10);
        keyValuePairs.Count.Should().Be(10);
        keyValuePairs[0].Should().Be(kvp1);
        keyValuePairs[1].Should().Be(kvp2);
        keyValuePairs[2].Should().Be(kvp3);
        keyValuePairs[3].Should().Be(kvp4);
        keyValuePairs[4].Should().Be(kvp5);
        keyValuePairs[5].Should().Be(kvp6);
        keyValuePairs[6].Should().Be(kvp7);
        keyValuePairs[7].Should().Be(kvp8);
        keyValuePairs[8].Should().Be(kvp9);
        keyValuePairs[9].Should().Be(kvp10);
        keyValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        keyValuePairs.Invoking(kvps => kvps[10]).Should().Throw<IndexOutOfRangeException>();
    }
}
