using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A mockable counterpart to <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>
/// and its <see cref="EventId"/> overload, exposing the same methods (minus the <see cref="ILogger"/>
/// receiver) as instance members instead of extension methods on <see cref="ILogger"/> - an extension
/// method can't be mocked or overridden, but instance members can. <typeparamref name="TCategoryName"/> is
/// used purely to select the log category, mirroring <see cref="ILogger{TCategoryName}"/> - it's never used
/// by any member. Obtain an implementation that forwards to a real <see cref="ILogger{TCategoryName}"/>
/// with <see cref="LoggerOperationExtensions.ToOperationLogger{TCategoryName}(ILogger{TCategoryName})"/>;
/// see <see cref="TestOperationLogger{TCategoryName}"/> for how to substitute a test double for
/// operation-logging behavior specifically.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the log category.</typeparam>
public interface IOperationLogger<out TCategoryName> : ILogger<TCategoryName>
{
    /// <inheritdoc cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>
    IOperationLog BeginOperation(string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false);

    /// <inheritdoc cref="LoggerOperationExtensions.BeginOperation(ILogger, EventId, string, LogLevel, bool)"/>
    IOperationLog BeginOperation(EventId eventId, string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false);
}
