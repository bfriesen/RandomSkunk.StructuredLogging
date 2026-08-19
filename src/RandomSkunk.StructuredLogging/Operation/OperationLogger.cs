using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The <see cref="IOperationLogger{TCategoryName}"/> implementation registered by
/// <see cref="OperationLoggerServiceCollectionExtensions.AddOperationLogger"/>. Just forwards each member
/// to the corresponding <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>
/// extension method on the injected <see cref="ILogger{TCategoryName}"/>.
/// </summary>
internal sealed class OperationLogger<TCategoryName>(ILogger<TCategoryName> logger) : IOperationLogger<TCategoryName>
{
    public IOperationLog BeginOperation(string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        logger.BeginOperation(name, level, threadSafe);

    public IOperationLog BeginOperation(EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        logger.BeginOperation(eventId, name, level, threadSafe);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) =>
        logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        logger.Log(logLevel, eventId, state, exception, formatter);
}
