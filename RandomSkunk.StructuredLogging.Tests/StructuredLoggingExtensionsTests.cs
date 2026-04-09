using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class StructuredLoggingExtensionsTests
{
    protected readonly Mock<EasyLogger<StructuredLoggingExtensionsTests>> _mockLogger = new();
    protected readonly ILogger<StructuredLoggingExtensionsTests> _logger;

    protected readonly EventId _eventId = new(42, "TestEvent");
    protected readonly Exception? _exception = new InvalidOperationException("Test exception");

    protected readonly (string Name, string Value) _logProperty1 = ("Foo", "abc");
    protected readonly (string Name, int Value) _logProperty2 = ("Bar", 123);
    protected readonly (string Name, bool Value) _logProperty3 = ("Baz", true);
    protected readonly (string Name, double Value) _logProperty4 = ("Qux", 45.6);
    protected readonly (string Name, string Value) _logProperty5 = ("Corge", "xyz");
    protected readonly (string Name, int Value) _logProperty6 = ("Grault", 789);
    protected readonly (string Name, bool Value) _logProperty7 = ("Garply", false);
    protected readonly (string Name, double Value) _logProperty8 = ("Waldo", 123.456);
    protected readonly (string Name, Guid Value) _logProperty9 = ("Plugh", Guid.Parse("8BDD00F1-6DB3-4760-862D-48643D76217C"));

    protected readonly string _message = "Test log message";
    protected readonly KeyValuePair<string, object?> _messageDataNameValuePair = new("Fred", new DateTime(2025, 11, 25, 21, 33, 34, 459, DateTimeKind.Utc));
    private protected readonly MessageData _messageData;

    protected LogLevel _logLevel = LogLevel.Information;

    protected StructuredLoggingExtensionsTests()
    {
        _logger = _mockLogger.Object;
        _messageData = new MessageData(_message, [_messageDataNameValuePair]);
    }

    public class WriteStructuredLogMethodWithNoLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyArray>>(state =>
                    state.Count == 1
                    && state[0].Key == _messageDataNameValuePair.Key
                    && Equals(state[0].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithOneLogProperty : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string>>>(state =>
                    state.Count == 2
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _messageDataNameValuePair.Key
                    && Equals(state[1].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithTwoLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int>>>(state =>
                    state.Count == 3
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _messageDataNameValuePair.Key
                    && Equals(state[2].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithThreeLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool>>>(state =>
                    state.Count == 4
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _messageDataNameValuePair.Key
                    && Equals(state[3].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithFourLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool, double>>>(state =>
                    state.Count == 5
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _messageDataNameValuePair.Key
                    && Equals(state[4].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithFiveLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool, double, string>>>(state =>
                    state.Count == 6
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _logProperty5.Name
                    && Equals(state[4].Value, _logProperty5.Value)
                    && state[5].Key == _messageDataNameValuePair.Key
                    && Equals(state[5].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithSixLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool, double, string, int>>>(state =>
                    state.Count == 7
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _logProperty5.Name
                    && Equals(state[4].Value, _logProperty5.Value)
                    && state[5].Key == _logProperty6.Name
                    && Equals(state[5].Value, _logProperty6.Value)
                    && state[6].Key == _messageDataNameValuePair.Key
                    && Equals(state[6].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithSevenLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool, double, string, int, bool>>>(state =>
                    state.Count == 8
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _logProperty5.Name
                    && Equals(state[4].Value, _logProperty5.Value)
                    && state[5].Key == _logProperty6.Name
                    && Equals(state[5].Value, _logProperty6.Value)
                    && state[6].Key == _logProperty7.Name
                    && Equals(state[6].Value, _logProperty7.Value)
                    && state[7].Key == _messageDataNameValuePair.Key
                    && Equals(state[7].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithEightLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7, in _logProperty8);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7, in _logProperty8);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logProperty1, in _logProperty2, in _logProperty3, in _logProperty4, in _logProperty5, in _logProperty6, in _logProperty7, in _logProperty8);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyTuple<string, int, bool, double, string, int, bool, double>>>(state =>
                    state.Count == 9
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _logProperty5.Name
                    && Equals(state[4].Value, _logProperty5.Value)
                    && state[5].Key == _logProperty6.Name
                    && Equals(state[5].Value, _logProperty6.Value)
                    && state[6].Key == _logProperty7.Name
                    && Equals(state[6].Value, _logProperty7.Value)
                    && state[7].Key == _logProperty8.Name
                    && Equals(state[7].Value, _logProperty8.Value)
                    && state[8].Key == _messageDataNameValuePair.Key
                    && Equals(state[8].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithNineLogProperties : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            Action act = () => logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logProperty1, _logProperty2, _logProperty3, _logProperty4, _logProperty5, _logProperty6, _logProperty7, _logProperty8, _logProperty9);

            // Act / Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenNullLogProperties_DoesNotThrow()
        {
            // Arrange
            Action act = () => _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, logProperties: ((string Name, object? Value)[])null!);

            // Act / Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logProperty1, _logProperty2, _logProperty3, _logProperty4, _logProperty5, _logProperty6, _logProperty7, _logProperty8, _logProperty9);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logProperty1, _logProperty2, _logProperty3, _logProperty4, _logProperty5, _logProperty6, _logProperty7, _logProperty8, _logProperty9);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogPropertyArray>>(state =>
                    state.Count == 10
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _logProperty3.Name
                    && Equals(state[2].Value, _logProperty3.Value)
                    && state[3].Key == _logProperty4.Name
                    && Equals(state[3].Value, _logProperty4.Value)
                    && state[4].Key == _logProperty5.Name
                    && Equals(state[4].Value, _logProperty5.Value)
                    && state[5].Key == _logProperty6.Name
                    && Equals(state[5].Value, _logProperty6.Value)
                    && state[6].Key == _logProperty7.Name
                    && Equals(state[6].Value, _logProperty7.Value)
                    && state[7].Key == _logProperty8.Name
                    && Equals(state[7].Value, _logProperty8.Value)
                    && state[8].Key == _logProperty9.Name
                    && Equals(state[8].Value, _logProperty9.Value)
                    && state[9].Key == _messageDataNameValuePair.Key
                    && Equals(state[9].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithNameValuePairCollection : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;
            Dictionary<string, object?> nameValuePairs = new()
            {
                { _logProperty1.Name, _logProperty1.Value },
                { _logProperty2.Name, _logProperty2.Value },
            };

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenNullNameValuePairs_DoesNotThrow()
        {
            // Arrange
            Action act = () => _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, logProperties: (IReadOnlyCollection<KeyValuePair<string, object?>>)null!);

            // Act / Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;
            Dictionary<string, object?> nameValuePairs = new()
            {
                { _logProperty1.Name, _logProperty1.Value },
                { _logProperty2.Name, _logProperty2.Value },
            };

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Arrange
            Dictionary<string, object?> nameValuePairs = new()
            {
                { _logProperty1.Name, _logProperty1.Value },
                { _logProperty2.Name, _logProperty2.Value },
            };

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<ReadOnlyNameValuePairCollection>>(state =>
                    state.Count == 3
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _messageDataNameValuePair.Key
                    && Equals(state[2].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithNameValuePairList : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;
            KeyValuePair<string, object?>[] nameValuePairs = 
            [
                new(_logProperty1.Name, _logProperty1.Value),
                new(_logProperty2.Name, _logProperty2.Value),
            ];

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenNullNameValuePairs_ThrowsArgumentNullException()
        {
            // Arrange
            Action act = () => _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, logProperties: (IReadOnlyList<KeyValuePair<string, object?>>)null!);

            // Act / Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("nameValuePairList");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;
            KeyValuePair<string, object?>[] nameValuePairs =
            [
                new(_logProperty1.Name, _logProperty1.Value),
                new(_logProperty2.Name, _logProperty2.Value),
            ];

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Arrange
            KeyValuePair<string, object?>[] nameValuePairs =
            [
                new(_logProperty1.Name, _logProperty1.Value),
                new(_logProperty2.Name, _logProperty2.Value),
            ];

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, nameValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<ReadOnlyNameValuePairList<KeyValuePair<string, object?>[]>>>(state =>
                    state.Count == 3
                    && state[0].Key == _logProperty1.Name
                    && Equals(state[0].Value, _logProperty1.Value)
                    && state[1].Key == _logProperty2.Name
                    && Equals(state[1].Value, _logProperty2.Value)
                    && state[2].Key == _messageDataNameValuePair.Key
                    && Equals(state[2].Value, _messageDataNameValuePair.Value)))),
                Times.Once());
        }
    }
}
