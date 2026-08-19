using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class TestOperationLoggerTests
{
    [Fact]
    public void ToOperationLogger_ForwardsBeginOperationToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        IOperationLogger<TestOperationLoggerTests> operationLogger = innerLogger.ToOperationLogger();

        using (operationLogger.BeginOperation("Name"))
        {
        }

        innerLogger.LogCallCount.Should().Be(1);
        innerLogger.LastMessage.Should().Be("Operation complete: Name");
    }

    [Fact]
    public void ToOperationLogger_NullLogger_ThrowsArgumentNullException()
    {
        ILogger<TestOperationLoggerTests> logger = null!;

        Func<IOperationLogger<TestOperationLoggerTests>> act = () => logger.ToOperationLogger();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsEnabled_ForwardsToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new() { Enabled = false };
        TestOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger);

        bool result = operationLogger.IsEnabled(LogLevel.Warning);

        result.Should().BeFalse();
    }

    [Fact]
    public void BeginScope_ForwardsToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        IDisposable scope = new FakeDisposable();
        innerLogger.ScopeToReturn = scope;
        TestOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger);

        IDisposable? result = operationLogger.BeginScope("state");

        innerLogger.LastScopeState.Should().Be("state");
        result.Should().BeSameAs(scope);
    }

    [Fact]
    public void Log_ForwardsToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        TestOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger);
        EventId eventId = new(42, "TestEvent");
        InvalidOperationException exception = new("boom");

        operationLogger.Log(LogLevel.Error, eventId, "state", exception, (state, ex) => $"{state}-{ex?.Message}");

        innerLogger.LogCallCount.Should().Be(1);
        innerLogger.LastLevel.Should().Be(LogLevel.Error);
        innerLogger.LastEventId.Should().Be(eventId);
        innerLogger.LastException.Should().BeSameAs(exception);
        innerLogger.LastMessage.Should().Be("state-boom");
    }

    [Fact]
    public void BeginOperation_DefaultImplementation_ReturnsNoOpAndDoesNotForwardToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        TestOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger);

        using (operationLogger.BeginOperation("Name"))
        {
        }

        innerLogger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void BeginOperationWithEventId_DefaultImplementation_ReturnsNoOpAndDoesNotForwardToInnerLogger()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        TestOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger);

        using (operationLogger.BeginOperation(new EventId(1, "Event"), "Name"))
        {
        }

        innerLogger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void BeginOperation_DefaultImplementation_CallsEventIdOverload()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        FakeOperationLog fakeOperation = new();
        RecordingEventIdOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger, fakeOperation);

        IOperationLog result = operationLogger.BeginOperation("Name");

        result.Should().BeSameAs(fakeOperation);
        operationLogger.LastOperationName.Should().Be("Name");
    }

    [Fact]
    public void BeginOperation_CanBeOverriddenByDerivedClass()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        FakeOperationLog fakeOperation = new();
        RecordingOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger, fakeOperation);

        IOperationLog result = operationLogger.BeginOperation("Name");

        result.Should().BeSameAs(fakeOperation);
        operationLogger.LastOperationName.Should().Be("Name");
        innerLogger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void BeginOperationWithEventId_CanBeOverriddenByDerivedClass()
    {
        RecordingLogger<TestOperationLoggerTests> innerLogger = new();
        FakeOperationLog fakeOperation = new();
        RecordingOperationLogger<TestOperationLoggerTests> operationLogger = new(innerLogger, fakeOperation);
        EventId eventId = new(1, "Event");

        IOperationLog result = operationLogger.BeginOperation(eventId, "Name");

        result.Should().BeSameAs(fakeOperation);
        operationLogger.LastOperationName.Should().Be("Name");
        innerLogger.LogCallCount.Should().Be(0);
    }

    private sealed class RecordingOperationLogger<T>(ILogger<T> logger, IOperationLog operationToReturn) : TestOperationLogger<T>(logger)
    {
        public string? LastOperationName { get; private set; }

        public override IOperationLog BeginOperation(string name, LogLevel level = LogLevel.Information, bool threadSafe = false)
        {
            LastOperationName = name;
            return operationToReturn;
        }

        public override IOperationLog BeginOperation(EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false)
        {
            LastOperationName = name;
            return operationToReturn;
        }
    }

    private sealed class RecordingEventIdOperationLogger<T>(ILogger<T> logger, IOperationLog operationToReturn) : TestOperationLogger<T>(logger)
    {
        public string? LastOperationName { get; private set; }

        public override IOperationLog BeginOperation(EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false)
        {
            LastOperationName = name;
            return operationToReturn;
        }
    }

    private sealed class FakeOperationLog : IOperationLog
    {
        public IReadOnlyList<KeyValuePair<string, object?>> Properties => Array.Empty<KeyValuePair<string, object?>>();

        public EventId EventId => default;

        public string OperationName => string.Empty;

        public IOperationLog AddProperty<T>(string name, T value) => this;

        public IOperationLog Append(string text) => this;

        public IOperationLog AppendValue<T>(T value, string? valueName = null) => this;

        public IOperationLog AppendJson<T>(T value, string? valueName = null) => this;

        public IOperationLog BeginSubOperation(string name) => throw new NotSupportedException();

        public IOperationLog SetException(Exception exception, bool recordEverywhere = false) => this;

        public IOperationLog SetResult<T>(T value) => this;

        public void Dispose()
        {
        }
    }

    private sealed class FakeDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly RecordingLogger _inner = new();

        public bool Enabled
        {
            get => _inner.Enabled;
            set => _inner.Enabled = value;
        }

        public IDisposable? ScopeToReturn
        {
            get => _inner.ScopeToReturn;
            set => _inner.ScopeToReturn = value;
        }

        public object? LastScopeState => _inner.LastScopeState;

        public int LogCallCount => _inner.LogCallCount;

        public LogLevel LastLevel => _inner.LastLevel;

        public EventId LastEventId => _inner.LastEventId;

        public Exception? LastException => _inner.LastException;

        public string? LastMessage => _inner.LastMessage;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
