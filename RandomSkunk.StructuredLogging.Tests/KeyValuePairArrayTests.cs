using FluentAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogAttributeArrayTests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsCountOfUnderlyingLogAttributeArrayLength()
        {
            // Arrange
            (string Key, object? Value)[] logAttributeArray =
            [
                ("Foo", "abc"),
                    ("Bar", 123)
            ];
            LogAttributeArray keyValuePairs = new(logAttributeArray);

            // Act
            int length = keyValuePairs.Length;

            // Assert
            length.Should().Be(logAttributeArray.Length);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, object? Value)[] logAttributeArray =
            [
                ("Foo", "abc"),
                    ("Bar", 123)
            ];
            LogAttributeArray keyValuePairs = new(logAttributeArray);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            actual.Key.Should().Be(logAttributeArray[index].Key);
            actual.Value.Should().Be(logAttributeArray[index].Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, object? Value)[] logAttributeArray =
            [
                ("Foo", "abc"),
                    ("Bar", 123)
            ];
            LogAttributeArray keyValuePairs = new(logAttributeArray);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple1Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsOne()
        {
            // Arrange
            (string Key, string Value) logAttribute = ("Foo", "abc");
            LogAttributeTuple<string> keyValuePairs = new(in logAttribute);

            // Act
            int length = keyValuePairs.Length;

            // Assert
            length.Should().Be(1);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute = ("Foo", "abc");
            LogAttributeTuple<string> keyValuePairs = new(in logAttribute);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            actual.Key.Should().Be(logAttribute.Key);
            actual.Value.Should().Be(logAttribute.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute = ("Foo", "abc");
            LogAttributeTuple<string> keyValuePairs = new(in logAttribute);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple2Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsTwo()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2);

            // Act
            int length = keyValuePairs.Length;

            // Assert
            length.Should().Be(2);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple3Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsThree()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple4Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsFour()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute4.Key);
                actual.Value.Should().Be(logAttribute4.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple5Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsFive()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logAttribute4.Key);
                actual.Value.Should().Be(logAttribute4.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute5.Key);
                actual.Value.Should().Be(logAttribute5.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple6Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsSix()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logAttribute4.Key);
                actual.Value.Should().Be(logAttribute4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logAttribute5.Key);
                actual.Value.Should().Be(logAttribute5.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute6.Key);
                actual.Value.Should().Be(logAttribute6.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(6)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple7Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsSeven()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logAttribute4.Key);
                actual.Value.Should().Be(logAttribute4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logAttribute5.Key);
                actual.Value.Should().Be(logAttribute5.Value);
            }
            else if (index == 5)
            {
                actual.Key.Should().Be(logAttribute6.Key);
                actual.Value.Should().Be(logAttribute6.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute7.Key);
                actual.Value.Should().Be(logAttribute7.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class LogAttributeTuple8Tests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsEight()
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            (string Key, double Value) logAttribute8 = ("Waldo", 123.456);
            LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8);

            // Act
            int length = keyValuePairs.Length;

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
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            (string Key, double Value) logAttribute8 = ("Waldo", 123.456);
            LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            if (index == 0)
            {
                actual.Key.Should().Be(logAttribute1.Key);
                actual.Value.Should().Be(logAttribute1.Value);
            }
            else if (index == 1)
            {
                actual.Key.Should().Be(logAttribute2.Key);
                actual.Value.Should().Be(logAttribute2.Value);
            }
            else if (index == 2)
            {
                actual.Key.Should().Be(logAttribute3.Key);
                actual.Value.Should().Be(logAttribute3.Value);
            }
            else if (index == 3)
            {
                actual.Key.Should().Be(logAttribute4.Key);
                actual.Value.Should().Be(logAttribute4.Value);
            }
            else if (index == 4)
            {
                actual.Key.Should().Be(logAttribute5.Key);
                actual.Value.Should().Be(logAttribute5.Value);
            }
            else if (index == 5)
            {
                actual.Key.Should().Be(logAttribute6.Key);
                actual.Value.Should().Be(logAttribute6.Value);
            }
            else if (index == 6)
            {
                actual.Key.Should().Be(logAttribute7.Key);
                actual.Value.Should().Be(logAttribute7.Value);
            }
            else
            {
                actual.Key.Should().Be(logAttribute8.Key);
                actual.Value.Should().Be(logAttribute8.Value);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(8)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            (string Key, string Value) logAttribute1 = ("Foo", "abc");
            (string Key, int Value) logAttribute2 = ("Bar", 123);
            (string Key, bool Value) logAttribute3 = ("Baz", true);
            (string Key, double Value) logAttribute4 = ("Qux", 45.6);
            (string Key, string Value) logAttribute5 = ("Corge", "xyz");
            (string Key, int Value) logAttribute6 = ("Grault", 789);
            (string Key, bool Value) logAttribute7 = ("Garply", false);
            (string Key, double Value) logAttribute8 = ("Waldo", 123.456);
            LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}

public class KeyValuePairCollectionTests
{
    public class LengthProperty
    {
        [Fact]
        public void ReturnsCountOfUnderlyingCollection()
        {
            // Arrange
            List<KeyValuePair<string, object?>> keyValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
            ];
            KeyValuePairCollection keyValuePairs = new(keyValuePairCollection);

            // Act
            int length = keyValuePairs.Length;

            // Assert
            length.Should().Be(keyValuePairCollection.Count);
        }
    }

    public class IndexerProperty
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> keyValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
            ];
            KeyValuePairCollection keyValuePairs = new(keyValuePairCollection);

            // Act
            KeyValuePair<string, object?> actual = keyValuePairs[index];

            // Assert
            actual.Should().Be(keyValuePairCollection[index]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
        {
            // Arrange
            List<KeyValuePair<string, object?>> keyValuePairCollection =
            [
                new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
            ];
            KeyValuePairCollection keyValuePairs = new(keyValuePairCollection);

            // Act
            Action act = () => _ = keyValuePairs[index];

            // Assert
            act.Should().Throw<IndexOutOfRangeException>();
        }
    }
}
