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
    /// <param name="name">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        BeginOperation(logger, default, name, level, threadSafe);

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
    /// <param name="name">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">The severity level the operation's final log entry is written at. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public static IOperationLog BeginOperation(this ILogger logger, EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!logger.IsEnabled(level))
        {
            IOperationLog disabledLog = new DisabledOperationLog(eventId);
            return threadSafe ? new SynchronizedOperationLog(disabledLog, new object()) : disabledLog;
        }

        OperationLogState state = new(logger, level, eventId, name);

        IOperationLog operationLog = new RootOperationLog(state, name);

        if (threadSafe)
            operationLog = new SynchronizedOperationLog(operationLog, state);

        return operationLog;
    }

    /// <summary>
    /// Wraps <paramref name="logger"/> in an <see cref="IOperationLogger{TCategoryName}"/> that forwards
    /// to it, for callers migrating from an injected <see cref="ILogger{TCategoryName}"/> to
    /// <see cref="IOperationLogger{TCategoryName}"/> without changing how the logger itself is obtained -
    /// e.g. <c>logger.ToOperationLogger().BeginOperation("FulfillOrder")</c> in place of
    /// <c>logger.BeginOperation("FulfillOrder")</c>. Because <see cref="IOperationLogger{TCategoryName}"/>
    /// extends <see cref="ILogger{TCategoryName}"/>, every other call site that used <paramref name="logger"/>
    /// as an <see cref="ILogger{TCategoryName}"/> keeps working unchanged against the wrapped result. See
    /// <see cref="TestOperationLogger{TCategoryName}"/> for how to substitute a test double for
    /// operation-logging behavior in tests, instead of this method's result.
    /// </summary>
    /// <typeparam name="TCategoryName">The type whose name is used for the log category.</typeparam>
    /// <param name="logger">The logger to wrap.</param>
    /// <returns>An <see cref="IOperationLogger{TCategoryName}"/> that forwards to <paramref name="logger"/>.</returns>
    public static IOperationLogger<TCategoryName> ToOperationLogger<TCategoryName>(this ILogger<TCategoryName> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        return new OperationLogger<TCategoryName>(logger);
    }
}
