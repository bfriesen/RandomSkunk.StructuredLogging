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
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// <see cref="ISubOperationLog"/> begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        BeginOperation(logger, default, name, level, threadSafe);

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
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// <see cref="ISubOperationLog"/> begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false)
    {
        ArgumentNullException.ThrowIfNull(logger);

        // A disabled operation is already a no-op, so there's nothing for threadSafe to protect -
        // skip the SynchronizedOperationLog wrap entirely rather than allocating a decorator around it.
        if (!logger.IsEnabled(level))
            return NullOperationLog.Instance;

        var state = new OperationLogState(logger, level, eventId);
        state.AppendLine("Operation started.");

        var operationLog = new RootOperationLog(state, name);

        if (threadSafe)
            return new SynchronizedOperationLog(operationLog, state);

        return operationLog;
    }
}
