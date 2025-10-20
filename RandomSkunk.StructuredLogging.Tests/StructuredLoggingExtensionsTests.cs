using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class StructuredLoggingExtensionsTests
{
    #region Write Method Tests - Basic LogAttributeSpanOrArray

    [Fact]
    public void Write_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger logger = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            logger.Write(LogLevel.Information, new EventId(1), null, "Test message", ("key", "value")));
    }

    [Fact]
    public void Write_WithLoggerDisabled_DoesNotLog()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        loggerMock.Object.MinimumLogLevel = LogLevel.Warning;
        ILogger logger = loggerMock.Object;

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test message", ("key", "value"));

        // Assert
        loggerMock.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never);
    }

    [Fact]
    public void Write_WithBasicParameters_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var eventId = new EventId(123, "TestEvent");
        var exception = new Exception("Test exception");

        // Act
        logger.Write(LogLevel.Error, eventId, exception, "Test message", ("foo", 123), ("bar", "abc"));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsError()
            && log.HasMessage("Test message")
            && log.HasAttribute("foo", 123)
            && log.HasAttribute("bar", "abc"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithNoAttributes_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test message without attributes");

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test message without attributes"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithNullMessage_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        EasyLogger logger = loggerMock.Object;
        logger.MinimumLogLevel = LogLevel.Debug;

        // Act
        logger.Write(LogLevel.Debug, new EventId(1), null, null, ("key", "value"));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsDebug()
            && log.HasAttribute("key", "value"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithNullAttributeKeys_FiltersNullKeys()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;

        // Act
        logger.Write(LogLevel.Warning, new EventId(1), null, "Test message", ("validKey", "value1"), (null!, "value2"), ("anotherValidKey", "value3"));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsWarning()
            && log.HasMessage("Test message")
            && log.HasAttribute("validKey", "value1")
            && log.HasAttribute("anotherValidKey", "value3"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithMultipleLogLevels_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        EasyLogger logger = loggerMock.Object;
        logger.MinimumLogLevel = LogLevel.Trace;

        // Act & Assert for each log level
        logger.Write(LogLevel.Trace, new EventId(1), null, "Trace message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsTrace())), Times.Once());

        logger.Write(LogLevel.Debug, new EventId(2), null, "Debug message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsDebug())), Times.Once());

        logger.Write(LogLevel.Information, new EventId(3), null, "Info message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsInformation())), Times.Once());

        logger.Write(LogLevel.Warning, new EventId(4), null, "Warning message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsWarning())), Times.Once());

        logger.Write(LogLevel.Error, new EventId(5), null, "Error message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsError())), Times.Once());

        logger.Write(LogLevel.Critical, new EventId(6), null, "Critical message");
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log => log.IsCritical())), Times.Once());
    }

    #endregion

    #region Write Method Tests - InterpolatedString

    [Fact]
    public void Write_WithInterpolatedString_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var value = 42;

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, $"Test with interpolated value: {value}", ("key", "value"));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test with interpolated value: 42")
            && log.HasAttribute("key", "value"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithInterpolatedStringAndException_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var exception = new InvalidOperationException("Test exception");
        var count = 5;

        // Act
        logger.Write(LogLevel.Error, new EventId(1), exception, $"Error processing {count} items", ("source", "test"));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsError()
            && log.HasMessage("Error processing 5 items")
            && log.HasAttribute("source", "test"))),
        Times.Once());
    }

    #endregion

    #region Write Method Tests - IReadOnlyCollection<KeyValuePair<string, object?>>

    [Fact]
    public void Write_WithKeyValuePairCollection_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("userId", 123),
            new("userName", "testUser"),
            new("isActive", true)
        };

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "User operation", attributes);

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("User operation")
            && log.HasAttribute("userId", 123)
            && log.HasAttribute("userName", "testUser")
            && log.HasAttribute("isActive", true))),
        Times.Once());
    }

    [Fact]
    public void Write_WithEmptyKeyValuePairCollection_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var attributes = new List<KeyValuePair<string, object?>>();

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test message", attributes);

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test message"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithNullKeyValuePairCollection_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        IReadOnlyCollection<KeyValuePair<string, object?>> attributes = null!;

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test message", attributes);

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test message"))),
        Times.Once());
    }

    [Fact]
    public void Write_WithInterpolatedStringAndKeyValuePairs_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var operationId = Guid.NewGuid();
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("duration", 250),
            new("status", "completed")
        };

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, $"Operation {operationId} finished", attributes);

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage($"Operation {operationId} finished")
            && log.HasAttribute("duration", 250)
            && log.HasAttribute("status", "completed"))),
        Times.Once());
    }

    #endregion

    #region BeginScope Method Tests - Basic LogAttributeSpanOrArray

    [Fact]
    public void BeginScope_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        ILogger logger = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            logger.BeginScope("Test scope", ("key", "value")));
    }

    [Fact]
    public void BeginScope_WithMessageAndAttributes_ReturnsScope()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;

        // Act
        var scope = logger.BeginScope("Test scope", ("key1", "value1"), ("key2", "value2"));

        // Assert
        Assert.NotNull(scope);
    }

    [Fact]
    public void BeginScope_WithOnlyMessage_ReturnsScope()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        EasyLogger logger = loggerMock.Object;

        // Act
        var scope = logger.BeginScope("Test scope");

        // Assert
        Assert.NotNull(scope);
    }

    [Fact]
    public void BeginScope_WithOnlyAttributes_ReturnsScope()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;

        // Act
        var scope = logger.BeginScope(("operation", "test"), ("requestId", 123));

        // Assert
        Assert.NotNull(scope);
    }

    [Fact]
    public void BeginScope_AttributesOnly_ReturnsScope()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;

        // Act
        var scope = logger.BeginScope(("traceId", "abc123"), ("spanId", "def456"));

        // Assert
        Assert.NotNull(scope);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Write_WithLargeNumberOfAttributes_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        
        // Create more than 8 attributes to test the default case
        var attributes = new List<(string, object?)>();
        for (int i = 0; i < 10; i++)
        {
            attributes.Add(($"key{i}", $"value{i}"));
        }

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test with many attributes", attributes.ToArray());

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test with many attributes")
            && log.HasAttribute("key0", "value0")
            && log.HasAttribute("key9", "value9"))),
        Times.Once());
    }

    [Fact]
    public void BeginScope_CanBeDisposed_WorksCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        EasyLogger logger = loggerMock.Object;

        // Act
        using (var scope = logger.BeginScope("Test disposable scope", ("resource", "database")))
        {
            // Assert
            Assert.NotNull(scope);

            Assert.Single(logger.CurrentScope);
            Assert.Collection(logger.CurrentScope, s => Assert.Equal("Test disposable scope", s.ToString()));
        } // Disposing should not throw.

        // After disposal, the scope should be removed.
        Assert.Empty(logger.CurrentScope);
    }

    [Fact]
    public void Write_WithVariousValueTypes_LogsCorrectly()
    {
        // Arrange
        Mock<EasyLogger> loggerMock = new();
        ILogger logger = loggerMock.Object;
        var dateTime = DateTime.Now;
        var guid = Guid.NewGuid();

        // Act
        logger.Write(LogLevel.Information, new EventId(1), null, "Test with various types",
            ("stringValue", "test"),
            ("intValue", 42),
            ("boolValue", true),
            ("doubleValue", 3.14),
            ("dateTimeValue", dateTime),
            ("guidValue", guid),
            ("nullValue", null));

        // Assert
        loggerMock.Verify(m => m.Write(It.Is<LogEntry>(log =>
            log.IsInformation()
            && log.HasMessage("Test with various types")
            && log.HasAttribute("stringValue", "test")
            && log.HasAttribute("intValue", 42)
            && log.HasAttribute("boolValue", true)
            && log.HasAttribute("doubleValue", 3.14)
            && log.HasAttribute("dateTimeValue", dateTime)
            && log.HasAttribute("guidValue", guid))),
        Times.Once());
    }

    #endregion
}
