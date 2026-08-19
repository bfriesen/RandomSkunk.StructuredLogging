using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A DI-friendly counterpart to <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>
/// and its <see cref="EventId"/> overload, exposing the same methods (minus the <see cref="ILogger"/>
/// receiver) as instance members instead of extension methods on <see cref="ILogger"/>. Constructor-inject
/// <see cref="IOperationLogger{TCategoryName}"/> instead of calling <c>ILogger.BeginOperation</c> directly
/// when a test needs to substitute a mock/fake in place of the real operation-logging behavior - an
/// extension method can't be mocked, but this interface can. <typeparamref name="TCategoryName"/> is used
/// purely to select the log category, mirroring <see cref="ILogger{TCategoryName}"/> - it's never used by
/// any member. Register the built-in implementation with
/// <see cref="OperationLoggerServiceCollectionExtensions.AddOperationLogger"/>.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the log category.</typeparam>
public interface IOperationLogger<out TCategoryName> : ILogger<TCategoryName>
{
    /// <inheritdoc cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>
    IOperationLog BeginOperation(string name, LogLevel level = LogLevel.Information, bool threadSafe = false);

    /// <inheritdoc cref="LoggerOperationExtensions.BeginOperation(ILogger, EventId, string, LogLevel, bool)"/>
    IOperationLog BeginOperation(EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false);
}
