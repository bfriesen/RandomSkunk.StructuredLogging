using FluentAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class LogAttributeArrayTests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeArray keyValuePairs = new(logAttributeArray, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(logAttributeArray.Length);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsSumOfUnderlyingLogAttributeArrayLengthAndMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, object? Value)[] logAttributeArray =
                [
                    ("Foo", "abc"),
                    ("Bar", 123)
                ];
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    new KeyValuePair<string, object?>("Baz", true)
                };
                LogAttributeArray keyValuePairs = new(logAttributeArray, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(logAttributeArray.Length + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeArray keyValuePairs = new(logAttributeArray, default);

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
                LogAttributeArray keyValuePairs = new(logAttributeArray, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(2)]
            public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
            {
                // Arrange
                (string Key, object? Value)[] logAttributeArray =
                [
                    ("Foo", "abc"),
                    ("Bar", 123)
                ];
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    additionalKeyValuePair
                };
                LogAttributeArray keyValuePairs = new(logAttributeArray, messageDataKeyValuePairs);

                // Act
                KeyValuePair<string, object?> actual = keyValuePairs[index];

                // Assert
                if (index < logAttributeArray.Length)
                {
                    actual.Key.Should().Be(logAttributeArray[index].Key);
                    actual.Value.Should().Be(logAttributeArray[index].Value);
                }
                else
                {
                    actual.Should().Be(additionalKeyValuePair);
                }
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(3)]
            public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
            {
                // Arrange
                (string Key, object? Value)[] logAttributeArray =
                [
                    ("Foo", "abc"),
                    ("Bar", 123)
                ];
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    additionalKeyValuePair
                };
                LogAttributeArray keyValuePairs = new(logAttributeArray, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                if (index == 3)
                    act.Should().Throw<IndexOutOfRangeException>();
                else
                    act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple1Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsOne()
            {
                // Arrange
                (string Key, string Value) logAttribute = ("Foo", "abc");
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(1);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsOnePlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute = ("Foo", "abc");
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    new KeyValuePair<string, object?>("Bar", 123)
                };
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(1 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Theory]
            [InlineData(0)]
            public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
            {
                // Arrange
                (string Key, string Value) logAttribute = ("Foo", "abc");
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, default);

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
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
            {
                // Arrange
                (string Key, string Value) logAttribute = ("Foo", "abc");
                KeyValuePair<string, object?> additionalKeyValuePair = new("Bar", 123);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, messageDataKeyValuePairs);

                // Act
                KeyValuePair<string, object?> actual = keyValuePairs[index];

                // Assert
                if (index == 0)
                {
                    actual.Key.Should().Be(logAttribute.Key);
                    actual.Value.Should().Be(logAttribute.Value);
                }
                else
                {
                    actual.Should().Be(additionalKeyValuePair);
                }
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(2)]
            public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
            {
                // Arrange
                (string Key, string Value) logAttribute = ("Foo", "abc");
                KeyValuePair<string, object?> additionalKeyValuePair = new("Bar", 123);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string> keyValuePairs = new(in logAttribute, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple2Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsTwo()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(2);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsTwoPlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(2 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, default);

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
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple3Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsThree()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(3);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsThreePlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                KeyValuePair<string, object?> additionalKeyValuePair = new("Qux", 45.6);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(3 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, default);

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
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Qux", 45.6);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Qux", 45.6);
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple4Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsFour()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                (string Key, double Value) logAttribute4 = ("Qux", 45.6);
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(4);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsFourPlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                (string Key, double Value) logAttribute4 = ("Qux", 45.6);
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(4 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, default);

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
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple5Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(5);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsFivePlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                (string Key, double Value) logAttribute4 = ("Qux", 45.6);
                (string Key, string Value) logAttribute5 = ("Corge", "xyz");
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(5 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, default);

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
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple6Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(6);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsSixPlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                (string Key, double Value) logAttribute4 = ("Qux", 45.6);
                (string Key, string Value) logAttribute5 = ("Corge", "xyz");
                (string Key, int Value) logAttribute6 = ("Grault", 789);
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(6 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, default);

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
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple7Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(7);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsSevenPlusMessageDataKeyValuePairsCount()
            {
                // Arrange
                (string Key, string Value) logAttribute1 = ("Foo", "abc");
                (string Key, int Value) logAttribute2 = ("Bar", 123);
                (string Key, bool Value) logAttribute3 = ("Baz", true);
                (string Key, double Value) logAttribute4 = ("Qux", 45.6);
                (string Key, string Value) logAttribute5 = ("Corge", "xyz");
                (string Key, int Value) logAttribute6 = ("Grault", 789);
                (string Key, bool Value) logAttribute7 = ("Garply", false);
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(7 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, default);

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
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, messageDataKeyValuePairs);

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
                    actual.Should().Be(additionalKeyValuePair);
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class LogAttributeTuple8Tests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(8);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsEightPlusMessageDataKeyValuePairsCount()
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(8 + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, default);

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
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
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
            [InlineData(8)]
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, messageDataKeyValuePairs);

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
                else if (index == 7)
                {
                    actual.Key.Should().Be(logAttribute8.Key);
                    actual.Value.Should().Be(logAttribute8.Value);
                }
                else
                {
                    actual.Should().Be(additionalKeyValuePair);
                }
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(9)]
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
                KeyValuePair<string, object?> additionalKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 20, 45, 30, 285, DateTimeKind.Utc));
                KeyValuePairList4 messageDataKeyValuePairs = new() { additionalKeyValuePair };
                LogAttributeTuple<string, int, bool, double, string, int, bool, double> keyValuePairs = new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}

public class KeyValuePairCollectionTests
{
    public class LengthProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, default);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(keyValuePairCollection.Count);
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Fact]
            public void ReturnsSumOfUnderlyingAndMessageDataKeyValuePairsCount()
            {
                // Arrange
                List<KeyValuePair<string, object?>> keyValuePairCollection =
                [
                    new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
                ];
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    new KeyValuePair<string, object?>("Baz", true)
                };
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, messageDataKeyValuePairs);

                // Act
                int length = keyValuePairs.Length;

                // Assert
                length.Should().Be(keyValuePairCollection.Count + messageDataKeyValuePairs.Count);
            }
        }
    }

    public class IndexerProperty
    {
        public class GivenNoMessageDataKeyValuePairs
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
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, default);

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
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, default);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }

        public class GivenMessageDataKeyValuePairs
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            [InlineData(2)]
            public void WhenIndexIsInRange_ReturnsExpectedKeyValuePair(int index)
            {
                // Arrange
                List<KeyValuePair<string, object?>> keyValuePairCollection =
                [
                    new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
                ];
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    additionalKeyValuePair
                };
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, messageDataKeyValuePairs);

                // Act
                KeyValuePair<string, object?> actual = keyValuePairs[index];

                // Assert
                if (index < keyValuePairCollection.Count)
                {
                    actual.Should().Be(keyValuePairCollection[index]);
                }
                else
                {
                    actual.Should().Be(additionalKeyValuePair);
                }
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(3)]
            public void WhenIndexIsOutOfRange_ThrowsIndexOutOfRangeException(int index)
            {
                // Arrange
                List<KeyValuePair<string, object?>> keyValuePairCollection =
                [
                    new KeyValuePair<string, object?>("Foo", "abc"),
                    new KeyValuePair<string, object?>("Bar", 123)
                ];
                KeyValuePair<string, object?> additionalKeyValuePair = new("Baz", true);
                KeyValuePairList4 messageDataKeyValuePairs = new()
                {
                    additionalKeyValuePair
                };
                KeyValuePairCollection keyValuePairs = new(keyValuePairCollection, messageDataKeyValuePairs);

                // Act
                Action act = () => _ = keyValuePairs[index];

                // Assert
                act.Should().Throw<IndexOutOfRangeException>();
            }
        }
    }
}
