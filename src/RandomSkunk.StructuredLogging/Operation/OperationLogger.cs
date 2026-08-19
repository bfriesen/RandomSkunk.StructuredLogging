using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The <see cref="IOperationLogger{TCategoryName}"/> implementation returned by
/// <see cref="LoggerOperationExtensions.ToOperationLogger{TCategoryName}(ILogger{TCategoryName})"/>. Just
/// forwards each member to the corresponding
/// <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/> extension
/// method on the injected <see cref="ILogger{TCategoryName}"/>. Sealed, with no virtual members - this is
/// the production implementation; see <see cref="TestOperationLogger{TCategoryName}"/> for a base class
/// test code can subclass to override operation-logging behavior.
/// </summary>
internal sealed class OperationLogger<TCategoryName>(ILogger<TCategoryName> logger) : IOperationLogger<TCategoryName>
{
    public IOperationLog BeginOperation(string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        logger.BeginOperation(operationName, level, threadSafe);

    public IOperationLog BeginOperation(EventId eventId, string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        logger.BeginOperation(eventId, operationName, level, threadSafe);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) =>
        logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        logger.Log(logLevel, eventId, state, exception, formatter);
}
