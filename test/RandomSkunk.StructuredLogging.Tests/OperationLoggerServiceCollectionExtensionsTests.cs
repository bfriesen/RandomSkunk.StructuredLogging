using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLoggerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOperationLogger_ReturnsSameServiceCollection()
    {
        FakeServiceCollection services = new();

        IServiceCollection result = services.AddOperationLogger();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddOperationLogger_RegistersOpenGenericSingleton()
    {
        FakeServiceCollection services = new();

        services.AddOperationLogger();

        ServiceDescriptor descriptor = services.Single();
        descriptor.ServiceType.Should().Be(typeof(IOperationLogger<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().NotBeNull();
        descriptor.ImplementationType!.GetInterfaces()
            .Should().Contain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOperationLogger<>));
    }

    [Fact]
    public void AddOperationLogger_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Func<IServiceCollection> act = () => services.AddOperationLogger();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteredImplementation_ForwardsToLoggerBeginOperation()
    {
        RecordingLogger<OperationLoggerServiceCollectionExtensionsTests> innerLogger = new();
        IOperationLogger<OperationLoggerServiceCollectionExtensionsTests> operationLogger = CreateOperationLogger(innerLogger);

        using (operationLogger.BeginOperation("Name"))
        {
        }

        innerLogger.LogCallCount.Should().Be(1);
        innerLogger.LastMessage.Should().Be("Operation complete: Name");
    }

    [Fact]
    public void RegisteredImplementation_IsEnabled_ForwardsToInnerLogger()
    {
        RecordingLogger<OperationLoggerServiceCollectionExtensionsTests> innerLogger = new() { Enabled = false };
        IOperationLogger<OperationLoggerServiceCollectionExtensionsTests> operationLogger = CreateOperationLogger(innerLogger);

        bool result = operationLogger.IsEnabled(LogLevel.Warning);

        result.Should().BeFalse();
    }

    [Fact]
    public void RegisteredImplementation_BeginScope_ForwardsToInnerLogger()
    {
        RecordingLogger<OperationLoggerServiceCollectionExtensionsTests> innerLogger = new();
        IDisposable scope = new FakeDisposable();
        innerLogger.ScopeToReturn = scope;
        IOperationLogger<OperationLoggerServiceCollectionExtensionsTests> operationLogger = CreateOperationLogger(innerLogger);

        IDisposable? result = operationLogger.BeginScope("state");

        innerLogger.LastScopeState.Should().Be("state");
        result.Should().BeSameAs(scope);
    }

    [Fact]
    public void RegisteredImplementation_Log_ForwardsToInnerLogger()
    {
        RecordingLogger<OperationLoggerServiceCollectionExtensionsTests> innerLogger = new();
        IOperationLogger<OperationLoggerServiceCollectionExtensionsTests> operationLogger = CreateOperationLogger(innerLogger);
        EventId eventId = new(42, "TestEvent");
        InvalidOperationException exception = new("boom");

        operationLogger.Log(LogLevel.Error, eventId, "state", exception, (state, ex) => $"{state}-{ex?.Message}");

        innerLogger.LogCallCount.Should().Be(1);
        innerLogger.LastLevel.Should().Be(LogLevel.Error);
        innerLogger.LastEventId.Should().Be(eventId);
        innerLogger.LastException.Should().BeSameAs(exception);
        innerLogger.LastMessage.Should().Be("state-boom");
    }

    private static IOperationLogger<OperationLoggerServiceCollectionExtensionsTests> CreateOperationLogger(
        RecordingLogger<OperationLoggerServiceCollectionExtensionsTests> innerLogger)
    {
        FakeServiceCollection services = new();
        services.AddOperationLogger();
        ServiceDescriptor descriptor = services.Single();

        Type closedType = descriptor.ImplementationType!.MakeGenericType(typeof(OperationLoggerServiceCollectionExtensionsTests));

        return (IOperationLogger<OperationLoggerServiceCollectionExtensionsTests>)Activator.CreateInstance(
            closedType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [innerLogger],
            culture: null)!;
    }

    private sealed class FakeServiceCollection : List<ServiceDescriptor>, IServiceCollection;

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
