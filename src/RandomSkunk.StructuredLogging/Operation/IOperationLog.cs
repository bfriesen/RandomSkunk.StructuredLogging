namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The root operation log returned by
/// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>.
/// Disposing it writes exactly one log entry summarizing everything that happened during the operation,
/// including any nested sub-operation activity. Every fluent member declared on
/// <see cref="IOperationLogBase{TOperationLog}"/> (the base shared with <see cref="ISubOperationLog"/>) returns
/// <see cref="IOperationLog"/> itself here, so a chain of calls on the root keeps returning the root. Adds
/// <see cref="SetException"/> and <see cref="SetResult{T}"/>, which set the
/// <c>Exception</c>/<c>Operation.Result</c> of that one eventual log entry - deliberately not part of
/// <see cref="ISubOperationLog"/> (the two interfaces are unrelated), so a sub-operation reference can
/// never reach in and silently overwrite the root's exception or result. A sub-operation that wants to
/// record its own outcome in the journal instead uses
/// <see cref="IOperationLogBase{TOperationLog}.AppendException"/>/<see cref="IOperationLogBase{TOperationLog}.AppendResult{T}"/>,
/// which the root also has - for a failure/result that should become the root's own
/// <c>Exception</c>/<c>Operation.Result</c>, hold onto the root <see cref="IOperationLog"/> itself (not a
/// sub-operation) and call <see cref="SetException"/>/<see cref="SetResult{T}"/> on it directly.
/// </summary>
public interface IOperationLog : IOperationLogBase<IOperationLog>
{
    /// <summary>
    /// Sets the <c>Exception</c> argument of the operation's final log entry. Unlike
    /// <see cref="IOperationLogBase{TOperationLog}.AppendException"/>, this doesn't append the exception itself
    /// (e.g. its stack trace) to the journal - it only appends a one-line "Operation exception set." marker
    /// (or "Operation exception set again, overwriting the previous value." on a second or later call), so the
    /// journal records *when* this was called and whether it happened more than once without duplicating the
    /// exception's full text. This does not, by itself, change the level the final log entry is written at - call
    /// <see cref="IOperationLogBase{TOperationLog}.Escalate"/> as well if the exception should also raise the
    /// operation's level. Calling this more than once overwrites any exception set by an earlier call - the
    /// journal marker above is the only warning that happens.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    IOperationLog SetException(Exception exception);

    /// <summary>
    /// Sets the <c>Operation.Result</c> structured property of the operation's final log entry. Unlike
    /// <see cref="IOperationLogBase{TOperationLog}.AppendResult{T}"/>, this doesn't append the formatted value
    /// itself to the journal - it only appends a one-line "Operation result set." marker (or "Operation result
    /// set again, overwriting the previous value." on a second or later call), so the journal records *when*
    /// this was called and whether it happened more than once without duplicating the formatted value.
    /// Typically called via the <see cref="OperationLogExtensions.SetResultTo{T}"/> extension
    /// method rather than directly, so it can be chained onto a return expression. Calling this more than
    /// once overwrites any result set by an earlier call - the journal marker above is the only warning that
    /// happens.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    IOperationLog SetResult<T>(T value);
}
