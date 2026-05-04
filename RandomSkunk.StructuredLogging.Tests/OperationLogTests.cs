using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class OperationLogTests
{
    private readonly Mock<EasyLogger> _mockLogger = new();
    private readonly ILogger _logger;

    public OperationLogTests()
    {
        _logger = _mockLogger.Object;
    }

    public class Constructor : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());

            Assert.Null(log.StringBuilder);
        }

        [Fact]
        public void GivenNoOperationName_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            Assert.Null(log.StringBuilder);
        }

        [Fact]
        public void GivenLoggerAndOperationName_WritesOperationStartingLog()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());

            Assert.NotNull(log.StringBuilder);
            Assert.Contains("Operation started", log.StringBuilder.ToString());
        }
    }

    public class DisposeMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_WritesOperationStartingLog()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.IsInformation()
                && log.EventId.Id == 456
                && log.HasMessage("Operation complete: My.Operation")
                && log.HasNoException()
                && log.HasAttribute("Foo", "abc")
                && log.HasAttribute("Bar", 123))), Times.Once());
        }
    }

    public class PropertiesProperty : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_ReturnsProperties()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());

            log.Properties.Should().HaveCount(2);
            log.Properties[0].Key.Should().Be("Foo");
            log.Properties[0].Value.Should().Be("abc");
            log.Properties[1].Key.Should().Be("Bar");
            log.Properties[1].Value.Should().Be(123);
        }

        [Fact]
        public void GivenNoOperationName_ReturnsProperties()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            log.Properties.Should().HaveCount(2);
            log.Properties[0].Key.Should().Be("Foo");
            log.Properties[0].Value.Should().Be("abc");
            log.Properties[1].Key.Should().Be("Bar");
            log.Properties[1].Value.Should().Be(123);
        }

        [Fact]
        public void GivenLoggerAndOperationName_ReturnsProperties()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());

            log.Properties.Should().HaveCount(2);
            log.Properties[0].Key.Should().Be("Foo");
            log.Properties[0].Value.Should().Be("abc");
            log.Properties[1].Key.Should().Be("Bar");
            log.Properties[1].Value.Should().Be(123);
        }
    }

    public class EventIdProperty : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_ReturnsEventId()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 123, "Operation complete: My.Operation", new());

            log.EventId.Id.Should().Be(123);
        }

        [Fact]
        public void GivenNoOperationName_ReturnsEventId()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 123, null, new());

            log.EventId.Id.Should().Be(123);
        }

        [Fact]
        public void GivenLoggerAndOperationName_ReturnsEventId()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 123, "Operation complete: My.Operation", new());

            log.EventId.Id.Should().Be(123);
        }
    }

    public class ReturnMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.ReturnValue(true).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.ReturnValue(true).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            const bool returnValue = true;
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.ReturnValue(returnValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.ReturnValue", true)
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains("Return value set to `returnValue`")))),
                Times.Once());
        }
    }

    public class IsNullMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            object? myValue = null;
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNull(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            object? myValue = null;
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.IsNull(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            object? myValue = null;
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNull(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains("`myValue` is null")))),
                Times.Once());
        }
    }

    public class IsNullOrEmptyMethod : OperationLogTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenNoLogger_UponDispose_DoesNothing(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNullOrEmpty(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenNoOperationName_UponDispose_DoesNothing(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.IsNullOrEmpty(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNullOrEmpty(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains("`myValue` is null or empty")))),
                Times.Once());
        }
    }

    public class IsNullOrWhiteSpaceMethod : OperationLogTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenNoLogger_UponDispose_DoesNothing(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenNoOperationName_UponDispose_DoesNothing(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete(string? myValue)
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains("`myValue` is null or whitespace")))),
                Times.Once());
        }
    }

    public class ExceptionMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var ex = new Exception();
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Exception(ex).Should().BeSameAs(ex);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var ex = new Exception();
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.Exception(ex).Should().BeSameAs(ex);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            InvalidOperationException ex = new();
            OperationLog<TwoItemNameValuePairList> log = new(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Exception(ex).Should().BeSameAs(ex);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasException(ex)
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains($"Exception set to `InvalidOperationException ex`")))),
                Times.Once());
        }
    }

    public class ConditionMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            const int x = 12345;
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Condition(x == 12345).Should().Be(true);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            const int x = 12345;
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            log.Condition(x == 12345).Should().Be(true);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            const int x = 12345;
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Condition(x == 12345).Should().Be(true);
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains($"`x == 12345` is true")))),
                Times.Once());
        }
    }

    public class LogMethod : OperationLogTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "Operation complete: My.Operation", new());

            log.Dispose();
            log.Append($"Hello, world!");
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            log.Dispose();
            log.Append($"Hello, world!");
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            var log = new OperationLog<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "Operation complete: My.Operation", new());
            log.Append($"Hello, world!");
            log.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("Operation.Log", value => value is string && ((string)value).Contains($"Hello, world!")))),
                Times.Once());
        }
    }
}
