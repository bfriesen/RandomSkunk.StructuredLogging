using FluentAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogPropertyArrayTests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsCountOfUnderlyingLogPropertyArrayLength()
        {
            // Arrange
            (string Key, object? Value)[] logPropertyArray =
            [
                ("Foo", "abc"),
                ("Bar", 123)
            ];
            LogPropertyArray nameValuePairs = new(logPropertyArray);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(logPropertyArray.Length);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, object? Value)[] logPropertyArray =
            [
                ("Foo", "abc"),
                ("Bar", 123)
            ];
            LogPropertyArray nameValuePairs = new(logPropertyArray);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            actual.Key.Should().Be(logPropertyArray[index].Key);
            actual.Value.Should().Be(logPropertyArray[index].Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, object? Value)[] logPropertyArray =
            [
                ("Foo", "abc"),
                ("Bar", 123)
            ];
            LogPropertyArray nameValuePairs = new(logPropertyArray);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple1Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsOne()
        {
            // Arrange
            (string Key, string Value) logProperty = ("Foo", "abc");
            LogPropertyTuple<string> nameValuePairs = new(in logProperty);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(1);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty = ("Foo", "abc");
            LogPropertyTuple<string> nameValuePairs = new(in logProperty);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            actual.Key.Should().Be(logProperty.Key);
            actual.Value.Should().Be(logProperty.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty = ("Foo", "abc");
            LogPropertyTuple<string> nameValuePairs = new(in logProperty);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple2Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsTwo()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            LogPropertyTuple<string, int> nameValuePairs = new(in logProperty1, in logProperty2);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(2);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            LogPropertyTuple<string, int> nameValuePairs = new(in logProperty1, in logProperty2);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            LogPropertyTuple<string, int> nameValuePairs = new(in logProperty1, in logProperty2);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple3Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsThree()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            LogPropertyTuple<string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(3);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            LogPropertyTuple<string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            LogPropertyTuple<string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple4Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsFour()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            LogPropertyTuple<string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(4);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            LogPropertyTuple<string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty4.Key);
                actual.Value.Should().Be(logProperty4.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            LogPropertyTuple<string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple5Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsFive()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            LogPropertyTuple<string, int, bool, double, string> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(5);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            LogPropertyTuple<string, int, bool, double, string> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logProperty4.Key);
                actual.Value.Should().Be(logProperty4.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty5.Key);
                actual.Value.Should().Be(logProperty5.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            LogPropertyTuple<string, int, bool, double, string> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple6Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsSix()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            LogPropertyTuple<string, int, bool, double, string, int> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(6);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            LogPropertyTuple<string, int, bool, double, string, int> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logProperty4.Key);
                actual.Value.Should().Be(logProperty4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logProperty5.Key);
                actual.Value.Should().Be(logProperty5.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty6.Key);
                actual.Value.Should().Be(logProperty6.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            LogPropertyTuple<string, int, bool, double, string, int> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple7Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsSeven()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            LogPropertyTuple<string, int, bool, double, string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(7);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            LogPropertyTuple<string, int, bool, double, string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logProperty4.Key);
                actual.Value.Should().Be(logProperty4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logProperty5.Key);
                actual.Value.Should().Be(logProperty5.Value);
            }
            else if (index == 5)
            {
                actual.Key.Should().Be(logProperty6.Key);
                actual.Value.Should().Be(logProperty6.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty7.Key);
                actual.Value.Should().Be(logProperty7.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            LogPropertyTuple<string, int, bool, double, string, int, bool> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogPropertyTuple8Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsEight()
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            (string Key, double Value) logProperty8 = ("Waldo", 123.456);
            LogPropertyTuple<string, int, bool, double, string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(8);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            (string Key, double Value) logProperty8 = ("Waldo", 123.456);
            LogPropertyTuple<string, int, bool, double, string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logProperty1.Key);
                actual.Value.Should().Be(logProperty1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logProperty2.Key);
                actual.Value.Should().Be(logProperty2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logProperty3.Key);
                actual.Value.Should().Be(logProperty3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logProperty4.Key);
                actual.Value.Should().Be(logProperty4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logProperty5.Key);
                actual.Value.Should().Be(logProperty5.Value);
            }
            else if (index == 5)
            {
                actual.Key.Should().Be(logProperty6.Key);
                actual.Value.Should().Be(logProperty6.Value);
            }
            else if (index == 6)
            {
                actual.Key.Should().Be(logProperty7.Key);
                actual.Value.Should().Be(logProperty7.Value);
            }
            else
            {
                actual.Key.Should().Be(logProperty8.Key);
                actual.Value.Should().Be(logProperty8.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(8)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logProperty1 = ("Foo", "abc");
            (string Key, int Value) logProperty2 = ("Bar", 123);
            (string Key, bool Value) logProperty3 = ("Baz", true);
            (string Key, double Value) logProperty4 = ("Qux", 45.6);
            (string Key, string Value) logProperty5 = ("Corge", "xyz");
            (string Key, int Value) logProperty6 = ("Grault", 789);
            (string Key, bool Value) logProperty7 = ("Garply", false);
            (string Key, double Value) logProperty8 = ("Waldo", 123.456);
            LogPropertyTuple<string, int, bool, double, string, int, bool, double> nameValuePairs = new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class ReadOnlyNameValuePairCollectionTests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsCountOfUnderlyingCollection()
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairCollection nameValuePairs = new(nameValuePairCollection);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(nameValuePairCollection.Count);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairCollection nameValuePairs = new(nameValuePairCollection);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            actual.Should().Be(nameValuePairCollection[index]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairCollection nameValuePairs = new(nameValuePairCollection);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class ReadOnlyNameValuePairListTests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsCountOfUnderlyingCollection()
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairList =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairList nameValuePairs = new(nameValuePairList);

            // Act
            int length = nameValuePairs.Length;

            // Assert
            length.Should().Be(nameValuePairList.Count);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedNameValuePair(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairList =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairList nameValuePairs = new(nameValuePairList);

            // Act
            KeyValuePair<string, object?> actual = nameValuePairs[index];

            // Assert
            actual.Should().Be(nameValuePairList[index]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsArgumentOutOfRangeException(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> nameValuePairList =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                new KeyValuePair<string, object?>("Bar", 123)
            ];
            ReadOnlyNameValuePairList nameValuePairs = new(nameValuePairList);

            // Act
            Action act = () => _ = nameValuePairs[index];

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
