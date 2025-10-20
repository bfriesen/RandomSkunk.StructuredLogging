using FluentAssertions;
using System.Collections;

namespace RandomSkunk.StructuredLogging.Tests;

using LogAttribute = (string Key, object? Value);

public class LogStateTests
{
    [Fact]
    public void Count_WithEmptyArray_ReturnsZero()
    {
        // Arrange
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(null));

        // Act
        int count = logState.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Count_WithNonEmptyArray_ReturnsCorrectCount()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));

        // Act
        int count = logState.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void Indexer_WithValidIndex_ReturnsCorrectKeyValuePair()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42), ("key3", null) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));

        // Act
        var kvp0 = logState[0];
        var kvp1 = logState[1];
        var kvp2 = logState[2];

        // Assert
        kvp0.Key.Should().Be("key1");
        kvp0.Value.Should().Be("value1");
        
        kvp1.Key.Should().Be("key2");
        kvp1.Value.Should().Be(42);
        
        kvp2.Key.Should().Be("key3");
        kvp2.Value.Should().BeNull();
    }

    [Fact]
    public void Indexer_WithInvalidIndex_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => logState[1]);
        Assert.Throws<IndexOutOfRangeException>(() => logState[-1]);
    }

    [Fact]
    public void ToString_ReturnsLogMessage()
    {
        // Arrange
        const string message = "Test log message";
        var logState = new LogState<LogAttributeArray>(message, new LogAttributeArray(null));

        // Act
        string? result = logState.ToString();

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void ToString_WithNullMessage_ReturnsNull()
    {
        // Arrange
        var logState = new LogState<LogAttributeArray>(null, new LogAttributeArray(null));

        // Act
        string? result = logState.ToString();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEnumerator_WithEmptyArray_ReturnsEmptyEnumerator()
    {
        // Arrange
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(null));

        // Act
        var enumerator = logState.GetEnumerator();
        var items = new List<KeyValuePair<string, object?>>();
        
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public void GetEnumerator_WithNonEmptyArray_EnumeratesAllItems()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42), ("key3", null) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));

        // Act
        var enumerator = logState.GetEnumerator();
        var items = new List<KeyValuePair<string, object?>>();
        
        while (enumerator.MoveNext())
        {
            items.Add(enumerator.Current);
        }

        // Assert
        items.Should().HaveCount(3);
        items[0].Should().Be(new KeyValuePair<string, object?>("key1", "value1"));
        items[1].Should().Be(new KeyValuePair<string, object?>("key2", 42));
        items[2].Should().Be(new KeyValuePair<string, object?>("key3", null));
    }

    [Fact]
    public void IEnumerable_GetEnumerator_WithNonEmptyArray_EnumeratesAllItems()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        IEnumerable<KeyValuePair<string, object?>> enumerable = logState;

        // Act
        var items = enumerable.ToList();

        // Assert
        items.Should().HaveCount(2);
        items[0].Should().Be(new KeyValuePair<string, object?>("key1", "value1"));
        items[1].Should().Be(new KeyValuePair<string, object?>("key2", 42));
    }

    [Fact]
    public void IEnumerable_NonGeneric_GetEnumerator_EnumeratesAllItems()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        IEnumerable enumerable = logState;

        // Act
        var items = new List<object>();
        foreach (var item in enumerable)
        {
            items.Add(item);
        }

        // Assert
        items.Should().HaveCount(2);
        items[0].Should().Be(new KeyValuePair<string, object?>("key1", "value1"));
        items[1].Should().Be(new KeyValuePair<string, object?>("key2", 42));
    }

    [Fact]
    public void ForEach_Loop_EnumeratesAllItems()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("userId", 123), ("userName", "John"), ("isActive", true) };
        var logState = new LogState<LogAttributeArray>("User logged in", new LogAttributeArray(attributes));

        // Act
        var items = new List<KeyValuePair<string, object?>>();
        foreach (var kvp in logState)
        {
            items.Add(kvp);
        }

        // Assert
        items.Should().HaveCount(3);
        items[0].Should().Be(new KeyValuePair<string, object?>("userId", 123));
        items[1].Should().Be(new KeyValuePair<string, object?>("userName", "John"));
        items[2].Should().Be(new KeyValuePair<string, object?>("isActive", true));
    }

    [Fact]
    public void LINQ_Operations_WorkCorrectly()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42), ("key3", null), ("key4", "value4") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));

        // Act
        var stringValues = logState
            .Where(kvp => kvp.Value is string)
            .Select(kvp => kvp.Key)
            .ToList();

        // Assert
        stringValues.Should().HaveCount(2);
        stringValues.Should().Contain("key1");
        stringValues.Should().Contain("key4");
    }

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    [Fact]
    public void IReadOnlyList_AsCollection_WorksCorrectly()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1"), ("key2", 42) };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        IReadOnlyList<KeyValuePair<string, object?>> readOnlyList = logState;

        // Act & Assert
        readOnlyList.Count.Should().Be(2);
        readOnlyList[0].Should().Be(new KeyValuePair<string, object?>("key1", "value1"));
        readOnlyList[1].Should().Be(new KeyValuePair<string, object?>("key2", 42));
    }
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

    [Fact]
    public void Enumerator_Current_BeforeMoveNext_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        var enumerator = logState.GetEnumerator();

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void Enumerator_MoveNext_BeyondEnd_ReturnsFalse()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        var enumerator = logState.GetEnumerator();

        // Act
        bool first = enumerator.MoveNext();
        bool second = enumerator.MoveNext();

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public void Enumerator_Reset_ThrowsNotSupportedException()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        IEnumerator enumerator = logState.GetEnumerator();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => enumerator.Reset());
    }

    [Fact]
    public void Enumerator_Dispose_DoesNotThrow()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        var enumerator = logState.GetEnumerator();

        // Act & Assert
        var action = () => ((IDisposable)enumerator).Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Enumerator_NonGeneric_Current_ReturnsCorrectType()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        IEnumerator enumerator = logState.GetEnumerator();

        // Act
        enumerator.MoveNext();
        var current = enumerator.Current;

        // Assert
        current.Should().BeOfType<KeyValuePair<string, object?>>();
        current.Should().Be(new KeyValuePair<string, object?>("key1", "value1"));
    }

    [Fact]
    public void Formatter_ReturnsToStringResult()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>("Test message", new LogAttributeArray(attributes));
        var exception = new Exception("Test exception");

        // Act
        string result = LogState<LogAttributeArray>.Formatter(logState, exception);

        // Assert
        result.Should().Be("Test message");
    }

    [Fact]
    public void Formatter_WithNullToString_ReturnsEmptyString()
    {
        // Arrange
        var attributes = new LogAttribute[] { ("key1", "value1") };
        var logState = new LogState<LogAttributeArray>(null, new LogAttributeArray(attributes));
        var exception = new Exception("Test exception");

        // Act
        string result = LogState<LogAttributeArray>.Formatter(logState, exception);

        // Assert
        result.Should().Be(string.Empty);
    }
}