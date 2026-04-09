using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class OperationTests
{
    private readonly Mock<EasyLogger<LogOperationExtensionsTests>> _mockLogger = new();
    private readonly ILogger<LogOperationExtensionsTests> _logger;

    public OperationTests()
    {
        _logger = _mockLogger.Object;
    }

    public class Constructor : OperationTests
    {
        [Fact]
        public void GivenNoLogger_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_WritesOperationStartingLog()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.IsInformation()
                && log.EventId.Id == 456
                && log.HasMessage("Operation starting: My.Operation")
                && log.HasNoException()
                && log.HasAttribute("Foo", "abc")
                && log.HasAttribute("Bar", 123))), Times.Once());
        }
    }

    public class DisposeMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_WritesOperationStartingLog()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.IsInformation()
                && log.EventId.Id == 456
                && log.HasMessage("Operation complete: My.Operation")
                && log.HasNoException()
                && log.HasAttribute("Foo", "abc")
                && log.HasAttribute("Bar", 123))), Times.Once());
        }
    }

    public class ParametersProperty : OperationTests
    {
        [Fact]
        public void GivenNoLogger_ReturnsParameters()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());

            op.Parameters.Should().HaveCount(2);
            op.Parameters[0].Key.Should().Be("Foo");
            op.Parameters[0].Value.Should().Be("abc");
            op.Parameters[1].Key.Should().Be("Bar");
            op.Parameters[1].Value.Should().Be(123);
        }

        [Fact]
        public void GivenNoOperationName_ReturnsParameters()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            op.Parameters.Should().HaveCount(2);
            op.Parameters[0].Key.Should().Be("Foo");
            op.Parameters[0].Value.Should().Be("abc");
            op.Parameters[1].Key.Should().Be("Bar");
            op.Parameters[1].Value.Should().Be(123);
        }

        [Fact]
        public void GivenLoggerAndOperationName_ReturnsParameters()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());

            op.Parameters.Should().HaveCount(2);
            op.Parameters[0].Key.Should().Be("Foo");
            op.Parameters[0].Value.Should().Be("abc");
            op.Parameters[1].Key.Should().Be("Bar");
            op.Parameters[1].Value.Should().Be(123);
        }
    }

    public class EventIdProperty : OperationTests
    {
        [Fact]
        public void GivenNoLogger_ReturnsEventId()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 123, "My.Operation", new());

            op.EventId.Id.Should().Be(123);
        }

        [Fact]
        public void GivenNoOperationName_ReturnsEventId()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 123, null, new());

            op.EventId.Id.Should().Be(123);
        }

        [Fact]
        public void GivenLoggerAndOperationName_ReturnsEventId()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 123, "My.Operation", new());

            op.EventId.Id.Should().Be(123);
        }
    }

    public class ReturnMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.Return(true).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.Return(true).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.Return(true).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("ReturnValue", true)
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains($"Return value of type System.Boolean set.")))),
                Times.Once());
        }
    }

    public class IsNullMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            object? myValue = null;
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.IsNull(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            object? myValue = null;
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.IsNull(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            object? myValue = null;
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.IsNull(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains("(myValue is null): true")))),
                Times.Once());
        }
    }

    public class IsNullOrEmptyMethod : OperationTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenNoLogger_UponDispose_DoesNothing(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.IsNullOrEmpty(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenNoOperationName_UponDispose_DoesNothing(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.IsNullOrEmpty(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.IsNullOrEmpty(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains("string.IsNullOrEmpty(myValue): true")))),
                Times.Once());
        }
    }

    public class IsNullOrWhiteSpaceMethod : OperationTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenNoLogger_UponDispose_DoesNothing(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenNoOperationName_UponDispose_DoesNothing(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete(string? myValue)
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.IsNullOrWhiteSpace(myValue).Should().BeTrue();
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains("string.IsNullOrWhiteSpace(myValue): true")))),
                Times.Once());
        }
    }

    public class ExceptionMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var ex = new Exception();
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.Exception(ex).Should().BeSameAs(ex);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var ex = new Exception();
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.Exception(ex).Should().BeSameAs(ex);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            var ex = new InvalidOperationException();
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.Exception(ex).Should().BeSameAs(ex);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasException(ex)
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains($"Exception of type System.InvalidOperationException set.")))),
                Times.Once());
        }
    }

    public class ConditionMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            const int x = 12345;
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());
            op.Condition(x == 12345).Should().Be(true);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            const int x = 12345;
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());
            op.Condition(x == 12345).Should().Be(true);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            const int x = 12345;
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.Condition(x == 12345).Should().Be(true);
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains($"(x == 12345): true")))),
                Times.Once());
        }
    }

    public class LogMethod : OperationTests
    {
        [Fact]
        public void GivenNoLogger_UponDispose_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(null, LogLevel.Information, 456, "My.Operation", new());

            op.Dispose();
            op.Log($"Hello, world!");
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenNoOperationName_UponDispose_DoesNothing()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, null, new());

            op.Dispose();
            op.Log($"Hello, world!");
            _mockLogger.Verify(m => m.Write(It.IsAny<LogEntry>()), Times.Never());
        }

        [Fact]
        public void GivenLoggerAndOperationName_UponDispose_LogsOperationComplete()
        {
            var op = new Operation<TwoItemNameValuePairList>(_logger, LogLevel.Information, 456, "My.Operation", new());
            op.Log($"Hello, world!");
            op.Dispose();

            _mockLogger.Verify(m => m.Write(It.Is<LogEntry>(log =>
                log.HasMessage("Operation complete: My.Operation")
                && log.HasAttribute("OperationLog", value => value is string && ((string)value).Contains($"Hello, world!")))),
                Times.Once());
        }
    }
}
