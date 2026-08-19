using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Extension methods for beginning an operation log on an <see cref="ILogger"/>.
/// </summary>
public static class LoggerOperationExtensions
{
    /// <summary>
    /// Begins an operation: a journal of everything that happens during it, written as exactly one log
    /// entry when the returned <see cref="IOperationLog"/> is disposed. If <paramref name="level"/> is
    /// disabled for <paramref name="logger"/>, no journal accumulation happens and no log entry is ever
    /// written - mirroring the level-disabled short-circuit this library's interpolated string handlers
    /// already use - but <see cref="IOperationLog.EventId"/> and <see cref="IOperationLog.Properties"/>
    /// (via <see cref="IOperationLog.AddProperty{T}"/>) still behave as they would on an enabled
    /// operation, since callers may read them regardless of whether the operation logs anything.
    /// </summary>
    /// <param name="logger">The logger the operation's single log entry will eventually be written to.</param>
    /// <param name="operationName">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        BeginOperation(logger, default, operationName, level, threadSafe);

    /// <summary>
    /// Begins an operation: a journal of everything that happens during it, written as exactly one log
    /// entry (tagged with <paramref name="eventId"/>) when the returned <see cref="IOperationLog"/> is
    /// disposed. If <paramref name="level"/> is disabled for <paramref name="logger"/>, no journal
    /// accumulation happens and no log entry is ever written - mirroring the level-disabled short-circuit
    /// this library's interpolated string handlers already use - but <see cref="IOperationLog.EventId"/>
    /// and <see cref="IOperationLog.Properties"/> (via <see cref="IOperationLog.AddProperty{T}"/>) still
    /// behave as they would on an enabled operation, since callers may read them regardless of whether
    /// the operation logs anything.
    /// </summary>
    /// <param name="logger">The logger the operation's single log entry will eventually be written to.</param>
    /// <param name="eventId">The event id associated with the operation's final log entry.</param>
    /// <param name="operationName">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, EventId eventId, string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logger.IsEnabled(level))
        {
            OperationLogState state = new(logger, level, eventId, operationName);
            RootOperationLog rootOperationLog = new(state, operationName);
            if (threadSafe)
                return new SynchronizedOperationLog(rootOperationLog, state);
            return rootOperationLog;
        }

        DisabledOperationLog disabledLog = new(eventId, operationName);
        if (threadSafe)
            return new SynchronizedOperationLog(disabledLog, disabledLog.Gate);
        return disabledLog;
    }
}
