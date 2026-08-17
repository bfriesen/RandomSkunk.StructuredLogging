using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension methods for beginning an operation log on an <see cref="ILogger"/>.
/// </summary>
public static class LoggerOperationExtensions
{
    /// <summary>
    /// Begins an operation: a journal of everything that happens during it, written as exactly one log
    /// entry when the returned <see cref="IOperationLog"/> is disposed. If <paramref name="level"/> is
    /// disabled for <paramref name="logger"/>, no journal/property accumulation happens at all - the
    /// returned <see cref="IOperationLog"/> is a no-op, mirroring the level-disabled short-circuit this
    /// library's interpolated string handlers already use.
    /// </summary>
    /// <param name="logger">The logger the operation's single log entry will eventually be written to.</param>
    /// <param name="name">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, string name, LogLevel level = LogLevel.Information) =>
        BeginOperation(logger, default, name, level);

    /// <summary>
    /// Begins an operation: a journal of everything that happens during it, written as exactly one log
    /// entry (tagged with <paramref name="eventId"/>) when the returned <see cref="IOperationLog"/> is
    /// disposed. If <paramref name="level"/> is disabled for <paramref name="logger"/>, no journal/property
    /// accumulation happens at all - the returned <see cref="IOperationLog"/> is a no-op, mirroring the
    /// level-disabled short-circuit this library's interpolated string handlers already use.
    /// </summary>
    /// <param name="logger">The logger the operation's single log entry will eventually be written to.</param>
    /// <param name="eventId">The event id associated with the operation's final log entry.</param>
    /// <param name="name">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, EventId eventId, string name, LogLevel level = LogLevel.Information)
    {
        if (!logger.IsEnabled(level))
            return NullOperationLog.Instance;

        var state = new OperationLogState(logger, level, eventId);

        lock (state)
            state.AppendLine("Operation started.");

        return new RootOperationLog(state, name);
    }
}
