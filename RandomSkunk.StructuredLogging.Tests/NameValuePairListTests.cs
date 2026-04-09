using FluentAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class NameValuePairList4Tests
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

        NameValuePairList2 nameValuePairs = default;
        nameValuePairs.Count.Should().Be(0);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[0]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp1);
        nameValuePairs.Count.Should().Be(1);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[1]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp2);
        nameValuePairs.Count.Should().Be(2);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[2]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp3);
        nameValuePairs.Count.Should().Be(3);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[3]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp4);
        nameValuePairs.Count.Should().Be(4);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[4]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp5);
        nameValuePairs.Count.Should().Be(5);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[5]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp6);
        nameValuePairs.Count.Should().Be(6);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[6]).Should().Throw<IndexOutOfRangeException>();
    }
}

public class NameValuePairList8Tests
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

        NameValuePairList6 nameValuePairs = default;
        nameValuePairs.Count.Should().Be(0);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[0]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp1);
        nameValuePairs.Count.Should().Be(1);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[1]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp2);
        nameValuePairs.Count.Should().Be(2);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[2]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp3);
        nameValuePairs.Count.Should().Be(3);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[3]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp4);
        nameValuePairs.Count.Should().Be(4);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[4]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp5);
        nameValuePairs.Count.Should().Be(5);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[5]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp6);
        nameValuePairs.Count.Should().Be(6);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[6]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp7);
        nameValuePairs.Count.Should().Be(7);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs[6].Should().Be(kvp7);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[7]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp8);
        nameValuePairs.Count.Should().Be(8);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs[6].Should().Be(kvp7);
        nameValuePairs[7].Should().Be(kvp8);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[8]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp9);
        nameValuePairs.Count.Should().Be(9);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs[6].Should().Be(kvp7);
        nameValuePairs[7].Should().Be(kvp8);
        nameValuePairs[8].Should().Be(kvp9);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[9]).Should().Throw<IndexOutOfRangeException>();

        nameValuePairs.Add(kvp10);
        nameValuePairs.Count.Should().Be(10);
        nameValuePairs[0].Should().Be(kvp1);
        nameValuePairs[1].Should().Be(kvp2);
        nameValuePairs[2].Should().Be(kvp3);
        nameValuePairs[3].Should().Be(kvp4);
        nameValuePairs[4].Should().Be(kvp5);
        nameValuePairs[5].Should().Be(kvp6);
        nameValuePairs[6].Should().Be(kvp7);
        nameValuePairs[7].Should().Be(kvp8);
        nameValuePairs[8].Should().Be(kvp9);
        nameValuePairs[9].Should().Be(kvp10);
        nameValuePairs.Invoking(kvps => kvps[-1]).Should().Throw<IndexOutOfRangeException>();
        nameValuePairs.Invoking(kvps => kvps[10]).Should().Throw<IndexOutOfRangeException>();
    }
}
