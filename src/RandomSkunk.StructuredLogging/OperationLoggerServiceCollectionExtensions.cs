using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension method for registering <see cref="IOperationLogger{TCategoryName}"/> with an
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class OperationLoggerServiceCollectionExtensions
{
    /// <summary>
    /// Registers the open generic <see cref="IOperationLogger{TCategoryName}"/> service, implemented by
    /// forwarding to <see cref="ILogger{TCategoryName}"/>'s <c>BeginOperation</c> extension methods, so it
    /// can be constructor-injected and, in tests, substituted with a mock/fake in its place. Registered as
    /// a singleton, matching how <c>AddLogging</c> itself registers the open generic
    /// <see cref="ILogger{TCategoryName}"/> service (<c>services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger&lt;&gt;), typeof(Logger&lt;&gt;)));</c>
    /// in <c>Microsoft.Extensions.Logging</c>'s <c>LoggingServiceCollectionExtensions</c>).
    /// </summary>
    /// <param name="services">The service collection to add the registration to.</param>
    /// <returns><paramref name="services"/>, so calls can be chained.</returns>
    public static IServiceCollection AddOperationLogger(this IServiceCollection services)
    {
        services.Add(ServiceDescriptor.Singleton(typeof(IOperationLogger<>), typeof(OperationLogger<>)));

        return services;
    }
}
