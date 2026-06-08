using Microsoft.Extensions.DependencyInjection;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension methods for adding operation logging services to an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds operation logging services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddOperationLogging(this IServiceCollection services) =>
        services.AddSingleton(typeof(IOperationLogger<>), typeof(OperationLogger<>))
            .AddScoped<IOperationLogAccessor, OperationLogAccessor>();
}

public interface IOperationLogAccessor
{
    IOperationLog? OperationLog { get; set; }
}

internal class OperationLogAccessor : IOperationLogAccessor
{
    IOperationLog? IOperationLogAccessor.OperationLog { get; set; }
}
