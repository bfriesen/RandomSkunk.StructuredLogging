using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class StructuredLoggingExtensionsTests
{
    protected readonly Mock<EasyLogger> _mockLogger = new();
    protected readonly ILogger _logger;

    protected readonly EventId _eventId = new(42, "TestEvent");
    protected readonly Exception? _exception = new InvalidOperationException("Test exception");

    protected readonly (string Key, string Value) _logAttribute1 = ("Foo", "abc");
    protected readonly (string Key, int Value) _logAttribute2 = ("Bar", 123);
    protected readonly (string Key, bool Value) _logAttribute3 = ("Baz", true);
    protected readonly (string Key, double Value) _logAttribute4 = ("Qux", 45.6);
    protected readonly (string Key, string Value) _logAttribute5 = ("Corge", "xyz");
    protected readonly (string Key, int Value) _logAttribute6 = ("Grault", 789);
    protected readonly (string Key, bool Value) _logAttribute7 = ("Garply", false);
    protected readonly (string Key, double Value) _logAttribute8 = ("Waldo", 123.456);
    protected readonly (string Key, Guid Value) _logAttribute9 = ("Plugh", Guid.Parse("8BDD00F1-6DB3-4760-862D-48643D76217C"));

    protected readonly string _message = "Test log message";
    protected readonly KeyValuePair<string, object?> _messageDataKeyValuePair = new("Fred", new DateTime(2025, 11, 25, 21, 33, 34, 459, DateTimeKind.Utc));
    private protected readonly MessageData _messageData;

    protected LogLevel _logLevel = LogLevel.Information;

    protected StructuredLoggingExtensionsTests()
    {
        _logger = _mockLogger.Object;
        _messageData = new MessageData(_message, [_messageDataKeyValuePair]);
    }

    public class WriteStructuredLogMethodWithNoLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
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
                && log.HasState<LogState<LogAttributeArray>>(state =>
                    state.Count == 1
                    && state[0].Key == _messageDataKeyValuePair.Key
                    && Equals(state[0].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithOneLogAttribute : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string>>>(state =>
                    state.Count == 2
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _messageDataKeyValuePair.Key
                    && Equals(state[1].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithTwoLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int>>>(state =>
                    state.Count == 3
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _messageDataKeyValuePair.Key
                    && Equals(state[2].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithThreeLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool>>>(state =>
                    state.Count == 4
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _messageDataKeyValuePair.Key
                    && Equals(state[3].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithFourLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool, double>>>(state =>
                    state.Count == 5
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _messageDataKeyValuePair.Key
                    && Equals(state[4].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithFiveLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool, double, string>>>(state =>
                    state.Count == 6
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _logAttribute5.Key
                    && Equals(state[4].Value, _logAttribute5.Value)
                    && state[5].Key == _messageDataKeyValuePair.Key
                    && Equals(state[5].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithSixLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool, double, string, int>>>(state =>
                    state.Count == 7
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _logAttribute5.Key
                    && Equals(state[4].Value, _logAttribute5.Value)
                    && state[5].Key == _logAttribute6.Key
                    && Equals(state[5].Value, _logAttribute6.Value)
                    && state[6].Key == _messageDataKeyValuePair.Key
                    && Equals(state[6].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithSevenLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool, double, string, int, bool>>>(state =>
                    state.Count == 8
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _logAttribute5.Key
                    && Equals(state[4].Value, _logAttribute5.Value)
                    && state[5].Key == _logAttribute6.Key
                    && Equals(state[5].Value, _logAttribute6.Value)
                    && state[6].Key == _logAttribute7.Key
                    && Equals(state[6].Value, _logAttribute7.Value)
                    && state[7].Key == _messageDataKeyValuePair.Key
                    && Equals(state[7].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithEightLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7, in _logAttribute8);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7, in _logAttribute8);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, in _logAttribute1, in _logAttribute2, in _logAttribute3, in _logAttribute4, in _logAttribute5, in _logAttribute6, in _logAttribute7, in _logAttribute8);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeTuple<string, int, bool, double, string, int, bool, double>>>(state =>
                    state.Count == 9
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _logAttribute5.Key
                    && Equals(state[4].Value, _logAttribute5.Value)
                    && state[5].Key == _logAttribute6.Key
                    && Equals(state[5].Value, _logAttribute6.Value)
                    && state[6].Key == _logAttribute7.Key
                    && Equals(state[6].Value, _logAttribute7.Value)
                    && state[7].Key == _logAttribute8.Key
                    && Equals(state[7].Value, _logAttribute8.Value)
                    && state[8].Key == _messageDataKeyValuePair.Key
                    && Equals(state[8].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithNineLogAttributes : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;

            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logAttribute1, _logAttribute2, _logAttribute3, _logAttribute4, _logAttribute5, _logAttribute6, _logAttribute7, _logAttribute8, _logAttribute9);

            // Act / Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenNullLogAttributes_DoesNotThrow()
        {
            // Arrange
            Action act = () => _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, logAttributes: null!);

            // Act / Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logAttribute1, _logAttribute2, _logAttribute3, _logAttribute4, _logAttribute5, _logAttribute6, _logAttribute7, _logAttribute8, _logAttribute9);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, _logAttribute1, _logAttribute2, _logAttribute3, _logAttribute4, _logAttribute5, _logAttribute6, _logAttribute7, _logAttribute8, _logAttribute9);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<LogAttributeArray>>(state =>
                    state.Count == 10
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _logAttribute3.Key
                    && Equals(state[2].Value, _logAttribute3.Value)
                    && state[3].Key == _logAttribute4.Key
                    && Equals(state[3].Value, _logAttribute4.Value)
                    && state[4].Key == _logAttribute5.Key
                    && Equals(state[4].Value, _logAttribute5.Value)
                    && state[5].Key == _logAttribute6.Key
                    && Equals(state[5].Value, _logAttribute6.Value)
                    && state[6].Key == _logAttribute7.Key
                    && Equals(state[6].Value, _logAttribute7.Value)
                    && state[7].Key == _logAttribute8.Key
                    && Equals(state[7].Value, _logAttribute8.Value)
                    && state[8].Key == _logAttribute9.Key
                    && Equals(state[8].Value, _logAttribute9.Value)
                    && state[9].Key == _messageDataKeyValuePair.Key
                    && Equals(state[9].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }

    public class WriteStructuredLogMethodWithKeyValuePairCollection : StructuredLoggingExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_ThrowsArgumentNullException()
        {
            // Arrange
            ILogger? logger = null;
            IReadOnlyList<KeyValuePair<string, object?>> keyValuePairs =
            [
                new(_logAttribute1.Key, _logAttribute1.Value),
                new(_logAttribute2.Key, _logAttribute2.Value),
            ];

            // Act
            Action act = () => logger!.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, keyValuePairs);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void GivenNullKeyValuePairs_DoesNotThrow()
        {
            // Arrange
            Action act = () => _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, keyValuePairs: null!);

            // Act / Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;
            IReadOnlyList<KeyValuePair<string, object?>> keyValuePairs =
            [
                new(_logAttribute1.Key, _logAttribute1.Value),
                new(_logAttribute2.Key, _logAttribute2.Value),
            ];

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, keyValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Arrange
            IReadOnlyCollection<KeyValuePair<string, object?>> keyValuePairs = new Dictionary<string, object?>
                {
                    { _logAttribute1.Key, _logAttribute1.Value },
                    { _logAttribute2.Key, _logAttribute2.Value },
                };

            // Act
            _logger.WriteStructuredLog(_logLevel, _eventId, _exception, _messageData, keyValuePairs);

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasEventId(_eventId)
                && log.HasException(_exception)
                && log.HasMessage(_message)
                && log.HasState<LogState<KeyValuePairCollection>>(state =>
                    state.Count == 3
                    && state[0].Key == _logAttribute1.Key
                    && Equals(state[0].Value, _logAttribute1.Value)
                    && state[1].Key == _logAttribute2.Key
                    && Equals(state[1].Value, _logAttribute2.Value)
                    && state[2].Key == _messageDataKeyValuePair.Key
                    && Equals(state[2].Value, _messageDataKeyValuePair.Value)))),
                Times.Once());
        }
    }
}
