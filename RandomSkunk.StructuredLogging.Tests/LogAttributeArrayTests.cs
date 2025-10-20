using FluentAssertions;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Tests;

using LogAttribute = (string Key, object? Value);

public class LogAttributeArrayTests
{
    #region LogAttributeArray Tests

    [Fact]
    public void LogAttributeArray_WithNullArray_ReturnsZeroSize()
    {
        // Arrange & Act
        var array = new LogAttributeArray(null);

        // Assert
        array.Size.Should().Be(0);
    }

    [Fact]
    public void LogAttributeArray_WithEmptyArray_ReturnsZeroSize()
    {
        // Arrange & Act
        var array = new LogAttributeArray([]);

        // Assert
        array.Size.Should().Be(0);
    }

    [Fact]
    public void LogAttributeArray_WithNonEmptyArray_ReturnsCorrectSize()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42) };

        // Act
        var array = new LogAttributeArray(attributes);

        // Assert
        array.Size.Should().Be(2);
    }

    [Fact]
    public void LogAttributeArray_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42), ("key3", null) };
        var array = new LogAttributeArray(attributes);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
        array.Get(2).Should().Be(("key3", null));
    }

    [Fact]
    public void LogAttributeArray_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var array = new LogAttributeArray(attributes);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(1));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    [Fact]
    public void LogAttributeArray_Get_WithNullArray_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray(null);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(0));
    }

    #endregion

#if NET9_0_OR_GREATER

    #region LogAttributeArray0 Tests

    [Fact]
    public void LogAttributeArray0_Size_ReturnsZero()
    {
        // Arrange & Act
        var array = new LogAttributeArray0();

        // Assert
        array.Size.Should().Be(0);
    }

    [Fact]
    public void LogAttributeArray0_Get_WithAnyIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray0();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(0));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(1));
    }

    #endregion

    #region LogAttributeArray1 Tests

    [Fact]
    public void LogAttributeArray1_Size_ReturnsOne()
    {
        // Arrange & Act
        var array = new LogAttributeArray1();

        // Assert
        array.Size.Should().Be(1);
    }

    [Fact]
    public void LogAttributeArray1_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray1();
        array[0] = ("testKey", "testValue");

        // Act & Assert
        array.Get(0).Should().Be(("testKey", "testValue"));
    }

    [Fact]
    public void LogAttributeArray1_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray1();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(1));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    [Fact]
    public void LogAttributeArray1_CanStoreAndRetrieveAttribute()
    {
        // Arrange
        var array = new LogAttributeArray1();
        var attribute = ("name", "John Doe");

        // Act
        array[0] = attribute;

        // Assert
        array[0].Should().Be(attribute);
        array.Get(0).Should().Be(attribute);
    }

    #endregion

    #region LogAttributeArray2 Tests

    [Fact]
    public void LogAttributeArray2_Size_ReturnsTwo()
    {
        // Arrange & Act
        var array = new LogAttributeArray2();

        // Assert
        array.Size.Should().Be(2);
    }

    [Fact]
    public void LogAttributeArray2_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray2();
        array[0] = ("key1", "value1");
        array[1] = ("key2", 42);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
    }

    [Fact]
    public void LogAttributeArray2_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray2();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(2));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    [Fact]
    public void LogAttributeArray2_CanStoreAndRetrieveMultipleAttributes()
    {
        // Arrange
        var array = new LogAttributeArray2();
        var attr1 = ("userId", 123);
        var attr2 = ("userName", "testUser");

        // Act
        array[0] = attr1;
        array[1] = attr2;

        // Assert
        array[0].Should().Be(attr1);
        array[1].Should().Be(attr2);
        array.Get(0).Should().Be(attr1);
        array.Get(1).Should().Be(attr2);
    }

    #endregion

    #region LogAttributeArray3 Tests

    [Fact]
    public void LogAttributeArray3_Size_ReturnsThree()
    {
        // Arrange & Act
        var array = new LogAttributeArray3();

        // Assert
        array.Size.Should().Be(3);
    }

    [Fact]
    public void LogAttributeArray3_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray3();
        array[0] = ("key1", "value1");
        array[1] = ("key2", 42);
        array[2] = ("key3", true);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
        array.Get(2).Should().Be(("key3", true));
    }

    [Fact]
    public void LogAttributeArray3_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray3();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(3));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

    #region LogAttributeArray4 Tests

    [Fact]
    public void LogAttributeArray4_Size_ReturnsFour()
    {
        // Arrange & Act
        var array = new LogAttributeArray4();

        // Assert
        array.Size.Should().Be(4);
    }

    [Fact]
    public void LogAttributeArray4_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray4();
        array[0] = ("key1", "value1");
        array[1] = ("key2", 42);
        array[2] = ("key3", true);
        array[3] = ("key4", null);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
        array.Get(2).Should().Be(("key3", true));
        array.Get(3).Should().Be(("key4", null));
    }

    [Fact]
    public void LogAttributeArray4_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray4();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(4));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

    #region LogAttributeArray5 Tests

    [Fact]
    public void LogAttributeArray5_Size_ReturnsFive()
    {
        // Arrange & Act
        var array = new LogAttributeArray5();

        // Assert
        array.Size.Should().Be(5);
    }

    [Fact]
    public void LogAttributeArray5_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray5();
        array[0] = ("key1", "value1");
        array[1] = ("key2", 42);
        array[2] = ("key3", true);
        array[3] = ("key4", null);
        array[4] = ("key5", 3.14);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
        array.Get(2).Should().Be(("key3", true));
        array.Get(3).Should().Be(("key4", null));
        array.Get(4).Should().Be(("key5", 3.14));
    }

    [Fact]
    public void LogAttributeArray5_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray5();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(5));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

    #region LogAttributeArray6 Tests

    [Fact]
    public void LogAttributeArray6_Size_ReturnsSix()
    {
        // Arrange & Act
        var array = new LogAttributeArray6();

        // Assert
        array.Size.Should().Be(6);
    }

    [Fact]
    public void LogAttributeArray6_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray6();
        array[0] = ("key1", "value1");
        array[1] = ("key2", 42);
        array[2] = ("key3", true);
        array[3] = ("key4", null);
        array[4] = ("key5", 3.14);
        array[5] = ("key6", DateTime.Now);

        // Act & Assert
        array.Get(0).Should().Be(("key1", "value1"));
        array.Get(1).Should().Be(("key2", 42));
        array.Get(2).Should().Be(("key3", true));
        array.Get(3).Should().Be(("key4", null));
        array.Get(4).Should().Be(("key5", 3.14));
        array.Get(5).Key.Should().Be("key6");
    }

    [Fact]
    public void LogAttributeArray6_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray6();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(6));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

    #region LogAttributeArray7 Tests

    [Fact]
    public void LogAttributeArray7_Size_ReturnsSeven()
    {
        // Arrange & Act
        var array = new LogAttributeArray7();

        // Assert
        array.Size.Should().Be(7);
    }

    [Fact]
    public void LogAttributeArray7_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray7();
        for (int i = 0; i < 7; i++)
        {
            array[i] = ($"key{i}", $"value{i}");
        }

        // Act & Assert
        for (int i = 0; i < 7; i++)
        {
            array.Get(i).Should().Be(($"key{i}", $"value{i}"));
        }
    }

    [Fact]
    public void LogAttributeArray7_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray7();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(7));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

    #region LogAttributeArray8 Tests

    [Fact]
    public void LogAttributeArray8_Size_ReturnsEight()
    {
        // Arrange & Act
        var array = new LogAttributeArray8();

        // Assert
        array.Size.Should().Be(8);
    }

    [Fact]
    public void LogAttributeArray8_Get_WithValidIndex_ReturnsCorrectAttribute()
    {
        // Arrange
        var array = new LogAttributeArray8();
        for (int i = 0; i < 8; i++)
        {
            array[i] = ($"key{i}", $"value{i}");
        }

        // Act & Assert
        for (int i = 0; i < 8; i++)
        {
            array.Get(i).Should().Be(($"key{i}", $"value{i}"));
        }
    }

    [Fact]
    public void LogAttributeArray8_Get_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var array = new LogAttributeArray8();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(8));
        Assert.Throws<IndexOutOfRangeException>(() => array.Get(-1));
    }

    #endregion

#endif

    #region ILogAttributeArray Interface Compliance Tests

    [Theory]
    [InlineData(typeof(LogAttributeArray))]
#if NET9_0_OR_GREATER
    [InlineData(typeof(LogAttributeArray0))]
    [InlineData(typeof(LogAttributeArray1))]
    [InlineData(typeof(LogAttributeArray2))]
    [InlineData(typeof(LogAttributeArray3))]
    [InlineData(typeof(LogAttributeArray4))]
    [InlineData(typeof(LogAttributeArray5))]
    [InlineData(typeof(LogAttributeArray6))]
    [InlineData(typeof(LogAttributeArray7))]
    [InlineData(typeof(LogAttributeArray8))]
#endif
    public void AllLogAttributeArrayTypes_ImplementILogAttributeArray(Type type)
    {
        // Assert
        type.Should().BeAssignableTo<ILogAttributeArray>();
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void AllInlineArrayTypes_HaveCorrectInlineArrayAttribute()
    {
        // Assert
        typeof(LogAttributeArray1).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 1);
        typeof(LogAttributeArray2).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 2);
        typeof(LogAttributeArray3).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 3);
        typeof(LogAttributeArray4).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 4);
        typeof(LogAttributeArray5).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 5);
        typeof(LogAttributeArray6).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 6);
        typeof(LogAttributeArray7).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 7);
        typeof(LogAttributeArray8).Should().BeDecoratedWith<InlineArrayAttribute>(attr => attr.Length == 8);
    }
#endif

    #endregion

    #region Edge Cases and Performance Tests

    [Fact]
    public void LogAttributeArray_WithLargeArray_PerformsCorrectly()
    {
        // Arrange
        const int arraySize = 1000;
        var attributes = new LogAttribute[arraySize];
        for (int i = 0; i < arraySize; i++)
        {
            attributes[i] = ($"key{i}", $"value{i}");
        }

        // Act
        var array = new LogAttributeArray(attributes);

        // Assert
        array.Size.Should().Be(arraySize);
        array.Get(0).Should().Be(("key0", "value0"));
        array.Get(arraySize - 1).Should().Be(($"key{arraySize - 1}", $"value{arraySize - 1}"));
    }

    [Fact]
    public void LogAttributeArray_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var attributes = new LogAttribute[] 
        { 
            ("key1", null), 
            ("key2", "value2"), 
            ("key3", null) 
        };

        // Act
        var array = new LogAttributeArray(attributes);

        // Assert
        array.Size.Should().Be(3);
        array.Get(0).Should().Be(("key1", null));
        array.Get(1).Should().Be(("key2", "value2"));
        array.Get(2).Should().Be(("key3", null));
    }

    #endregion
}