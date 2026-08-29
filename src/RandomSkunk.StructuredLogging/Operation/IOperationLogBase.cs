using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Declares the members shared by <see cref="IOperationLog"/> (the root operation) and
/// <see cref="ISubOperationLog"/> (a nested sub-operation), each fluent member returning
/// <typeparamref name="TOperationLog"/> itself so a chain of calls on an <see cref="IOperationLog"/> keeps
/// returning <see cref="IOperationLog"/> (and likewise for <see cref="ISubOperationLog"/>) rather than
/// widening to some common ancestor type. <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/>
/// are deliberately *not* related to each other - each extends this interface independently - so that
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>, which only
/// <see cref="IOperationLog"/> declares, can never be reached from a sub-operation reference. A sub-operation
/// that wants to record its own outcome uses <see cref="AppendException"/>/<see cref="AppendResult{T}"/>
/// instead, declared here so both the root and every sub-operation have them.
/// </summary>
/// <typeparam name="TOperationLog">
/// The most-derived interface - <see cref="IOperationLog"/> or <see cref="ISubOperationLog"/> - so this
/// interface's fluent members return that same type rather than a shared base type.
/// </typeparam>
public interface IOperationLogBase<TOperationLog> : IDisposable
    where TOperationLog : IOperationLogBase<TOperationLog>
{
    /// <summary>
    /// The structured properties added so far via <see cref="AddProperty{T}"/>, on this operation or any
    /// other operation in the same tree (root or sub-operation) - they all share the same eventual log
    /// entry. Empty if <see cref="AddProperty{T}"/> has never been called. For an operation begun with
    /// <c>threadSafe: true</c>, this returns a point-in-time snapshot rather than a live view, so it's safe
    /// to enumerate even while another thread concurrently calls <see cref="AddProperty{T}"/>.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, object?>> Properties { get; }

    /// <summary>
    /// The <see cref="EventId"/> the operation was begun with (via the
    /// <c>BeginOperation(eventId, operationName, ...)</c> overload), or <c>default</c> if the operation was begun
    /// without one. The same value on the root operation and every nested sub-operation, since it's the
    /// <see cref="EventId"/> that ends up on the one eventual log entry. Useful for tying a log line
    /// written elsewhere (e.g. from within the operation) back to the operation's own final entry.
    /// </summary>
    EventId EventId { get; }

    /// <summary>
    /// Whether this operation is actually journaling - <see langword="false"/> if the level passed to
    /// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>
    /// was disabled on the logger at that time, <see langword="true"/> otherwise. The same value on the
    /// root operation and every nested sub-operation, since <see cref="BeginSubOperation(string)"/> always
    /// produces a sub-operation that matches its parent. Never changes after the operation begins - in
    /// particular, <see cref="Escalate"/> can raise the level the final entry is written at, but it can't
    /// turn a disabled operation into an enabled one. Useful for skipping expensive work that would only
    /// go into an <see cref="AddProperty{T}"/>/<see cref="AppendValue{T}"/>/<see cref="AppendJson{T}"/>
    /// call whose result would otherwise be discarded, e.g. <c>if (log.IsEnabled) log.AppendJson(BuildExpensiveDiagnostics());</c>.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Raises the level the operation's final log entry is written at, if <paramref name="level"/> is more
    /// severe than the operation's current level - otherwise this is a no-op. Unlike the level passed to
    /// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>,
    /// which also determines up front whether the operation journals anything at all, this can only raise
    /// the level of an already-enabled operation - it never re-enables a disabled one. Can be called on the
    /// root operation or any nested sub-operation; either way it affects the one level the eventual entry
    /// gets written at, the same way <see cref="AddProperty{T}"/> affects the one set of properties.
    /// Typically called alongside <see cref="AppendException"/> (or, on the root, <see cref="IOperationLog.SetException"/>),
    /// but useful on its own too - e.g. a business failure that never throws (a rejected/backordered/declined
    /// result) can still warrant a higher level.
    /// </summary>
    /// <param name="level">The level to escalate to, if more severe than the operation's current level.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog Escalate(LogLevel level);

    /// <summary>
    /// Adds a structured property to the operation's final log entry. Unlike the built-in
    /// <c>Operation.*</c> properties (<c>Operation.Name</c>, <c>Operation.StartTime</c>,
    /// <c>Operation.DurationSeconds</c>, <c>Operation.Result</c>), properties set here are added unprefixed.
    /// Can be called on the root operation or any nested
    /// sub-operation; either way the property is added to the one entry that eventually gets flushed.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog AddProperty<T>(string name, T value);

    /// <summary>
    /// Appends a "`name` failed: ..." line describing <paramref name="exception"/> to the journal (the root
    /// operation writes "Operation failed: ..." instead, since it has no sub-operation name of its own).
    /// Unlike <see cref="IOperationLog.SetException"/>, this never sets the <c>Exception</c> argument of the
    /// final log entry - only the root's own <see cref="IOperationLog.SetException"/> can do that. This does
    /// not, by itself, change the level the final log entry is written at - call <see cref="Escalate"/> as
    /// well if the exception should also raise the operation's level.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog AppendException(Exception exception);

    /// <summary>
    /// Appends a "`name` result: ..." line (rendered via <see cref="IFormattable"/>/<see cref="object.ToString"/>)
    /// describing <paramref name="value"/> to the journal (the root operation writes "Operation result: ..."
    /// instead, since it has no sub-operation name of its own). Unlike <see cref="IOperationLog.SetResult{T}"/>,
    /// this never sets the <c>Operation.Result</c> structured property of the final log entry - only the
    /// root's own <see cref="IOperationLog.SetResult{T}"/> can do that. Typically called via the
    /// <see cref="OperationLogExtensions.AppendResultTo{T, TLog}"/> extension method rather than directly, so
    /// it can be chained onto a return expression.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog AppendResult<T>(T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog Append(string text);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry. Unlike <see cref="Append(string)"/>, <paramref name="text"/>'s interpolated
    /// arguments are only evaluated if <see cref="IsEnabled"/> is <see langword="true"/> - see
    /// <see cref="OperationLogInterpolatedStringHandler"/>.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog Append([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler text);

    /// <summary>
    /// Appends a line of free text to the operation's journal in the form <c>`valueName`: value</c>,
    /// rendering <paramref name="value"/> via <see cref="IFormattable"/>/<see cref="object.ToString"/>.
    /// <paramref name="valueName"/> defaults to the source text of the <paramref name="value"/> argument
    /// expression, so <c>log.AppendValue(order.Total)</c> appends a line like <c>`order.Total`: 42.50</c>
    /// without having to spell the name out explicitly.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Appends a line of free text to the operation's journal in the form <c>`valueName`: value</c>, like
    /// <see cref="AppendValue{T}"/>, but rendering <paramref name="value"/> as indented JSON (via
    /// <see cref="System.Text.Json.JsonSerializer"/>) instead of via
    /// <see cref="IFormattable"/>/<see cref="object.ToString"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns>This <typeparamref name="TOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    TOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Begins a nested sub-operation. A "started" line is immediately appended to the journal, and a
    /// "complete" line is appended when the returned <see cref="ISubOperationLog"/> is disposed. Like this
    /// operation, the returned sub-operation never writes its own log entry - it only ever contributes to
    /// the root's single flushed entry. Always returns <see cref="ISubOperationLog"/>, regardless of
    /// <typeparamref name="TOperationLog"/> - a sub-operation of the root is still just a sub-operation.
    /// </summary>
    /// <param name="operationName">The sub-operation's name, used in its journal lines (e.g. "started"/"complete").</param>
    /// <returns>An <see cref="ISubOperationLog"/> representing the nested sub-operation.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    ISubOperationLog BeginSubOperation(string operationName);

    /// <summary>
    /// Begins a nested sub-operation, like <see cref="BeginSubOperation(string)"/>. Unlike that overload,
    /// <paramref name="operationName"/>'s interpolated arguments are only evaluated if
    /// <see cref="IsEnabled"/> is <see langword="true"/> - see
    /// <see cref="OperationLogInterpolatedStringHandler"/>. Useful when building the sub-operation's name
    /// is itself non-trivial and shouldn't be paid for on a disabled operation.
    /// </summary>
    /// <param name="operationName">The sub-operation's name, used in its journal lines (e.g. "started"/"complete").</param>
    /// <returns>An <see cref="ISubOperationLog"/> representing the nested sub-operation.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    ISubOperationLog BeginSubOperation([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler operationName);
}
