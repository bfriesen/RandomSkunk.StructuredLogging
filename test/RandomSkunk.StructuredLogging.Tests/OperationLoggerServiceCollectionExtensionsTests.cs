using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLoggerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOperationLogger_ReturnsSameServiceCollection()
    {
        var services = new FakeServiceCollection();

        var result = services.AddOperationLogger();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddOperationLogger_RegistersOpenGenericSingleton()
    {
        var services = new FakeServiceCollection();

        services.AddOperationLogger();

        var descriptor = services.Single();
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

        var act = () => services.AddOperationLogger();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteredImplementation_ForwardsToLoggerBeginOperation()
    {
        var services = new FakeServiceCollection();
        services.AddOperationLogger();
        var descriptor = services.Single();

        var closedType = descriptor.ImplementationType!.MakeGenericType(typeof(OperationLoggerServiceCollectionExtensionsTests));
        var innerLogger = new RecordingLogger<OperationLoggerServiceCollectionExtensionsTests>();

        var operationLogger = (IOperationLogger<OperationLoggerServiceCollectionExtensionsTests>)Activator.CreateInstance(
            closedType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [innerLogger],
            culture: null)!;

        using (operationLogger.BeginOperation("Name"))
        {
        }

        innerLogger.LogCallCount.Should().Be(1);
        innerLogger.LastMessage.Should().Be("Operation complete: Name");
    }

    private sealed class FakeServiceCollection : List<ServiceDescriptor>, IServiceCollection;

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly RecordingLogger _inner = new();

        public int LogCallCount => _inner.LogCallCount;

        public string? LastMessage => _inner.LastMessage;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
