using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class LogOperationExtensionsTests
{
    protected readonly Mock<EasyLogger<LogOperationExtensionsTests>> _mockLogger = new();
    protected readonly ILogger<LogOperationExtensionsTests> _logger;

    protected readonly EventId _eventId = new(42, "TestEvent");

    protected LogLevel _logLevel = LogLevel.Information;

    protected LogOperationExtensionsTests()
    {
        _logger = _mockLogger.Object;
    }

    public class GetOperationMethodWithNoLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            var log = logger!.LogOperation(_logLevel, _eventId, "My.Operation");

            // Assert
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().BeEmpty();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation");

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().BeEmpty();
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation");

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation"))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().BeEmpty();
        }
    }

    public class GetOperationMethodWithOneLogProperty : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(1);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(1);
        }
    }

    public class GetOperationMethodWithTwoLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(2);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(2);
        }
    }

    public class GetOperationMethodWithThreeLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(3);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(3);
        }
    }

    public class GetOperationMethodWithFourLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(4);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3)
                && log.HasAttribute("P4", 4))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(4);
        }
    }

    public class GetOperationMethodWithFiveLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(5);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3)
                && log.HasAttribute("P4", 4)
                && log.HasAttribute("P5", 5))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(5);
        }
    }

    public class GetOperationMethodWithSixLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(6);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3)
                && log.HasAttribute("P4", 4)
                && log.HasAttribute("P5", 5)
                && log.HasAttribute("P6", 6))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(6);
        }
    }

    public class GetOperationMethodWithSevenLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(7);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3)
                && log.HasAttribute("P4", 4)
                && log.HasAttribute("P5", 5)
                && log.HasAttribute("P6", 6)
                && log.HasAttribute("P7", 7))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(7);
        }
    }

    public class GetOperationMethodWithEightLogProperties : LogOperationExtensionsTests
    {
        [Fact]
        public void GivenNullLogger_DoesNotThrow()
        {
            // Arrange
            ILogger? logger = null;

            // Act
            Action act = () => logger!.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("P8", 8));

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GivenLoggerIsDisabledAtTheSpecifiedLogLevel_DoesNotInvokeLogMethod()
        {
            // Arrange
            _logLevel = LogLevel.Debug;

            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("P8", 8));

            // Assert
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(8);
        }

        [Fact]
        public void GivenLoggerIsEnabledAtTheSpecifiedLogLevel_InvokesLogMethod()
        {
            // Act
            var log = _logger.LogOperation(_logLevel, _eventId, "My.Operation", ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("P8", 8));

            // Assert
            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasLogLevel(_logLevel)
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasAttribute("P1", 1)
                && log.HasAttribute("P2", 2)
                && log.HasAttribute("P3", 3)
                && log.HasAttribute("P4", 4)
                && log.HasAttribute("P5", 5)
                && log.HasAttribute("P6", 6)
                && log.HasAttribute("P7", 7)
                && log.HasAttribute("P8", 8))), Times.Once());
            log.EventId.Should().Be(_eventId);
            log.Properties.Should().HaveCount(8);
        }
    }
}
