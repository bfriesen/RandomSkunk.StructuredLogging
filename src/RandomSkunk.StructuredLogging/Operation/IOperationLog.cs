using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Represents an in-progress operation (or nested sub-operation) being journaled by
/// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>.
/// Disposing the root <see cref="IOperationLog"/> writes exactly one log entry summarizing everything that
/// happened during the operation, including any nested sub-operation activity. A sub-operation, returned by
/// <see cref="BeginSubOperation"/>, never writes its own log entry - disposing it appends a "complete" line
/// to the ancestor journal it was created from instead.
/// Every sub-operation shares the root operation's journal, so once the root <see cref="IOperationLog"/> has
/// been disposed, calling any member other than <see cref="IDisposable.Dispose"/>, <see cref="Properties"/>,
/// or <see cref="EventId"/> on a sub-operation still referenced from outside the root's <c>using</c> scope
/// throws <see cref="ObjectDisposedException"/>, rather than risking corruption of an unrelated operation's
/// journal.
/// </summary>
public interface IOperationLog : IDisposable
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
    /// Records the exception for this operation. On the root operation, this becomes the <c>Exception</c>
    /// argument of the final log entry. On a sub-operation, this instead appends a "failed" line describing
    /// it to the journal. This does not, by itself, change the level the final log entry is written at -
    /// call <see cref="Escalate"/> as well if the exception should also raise the operation's level.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog SetException(Exception exception);

    /// <summary>
    /// Raises the level the operation's final log entry is written at, if <paramref name="level"/> is more
    /// severe than the operation's current level - otherwise this is a no-op. Unlike the level passed to
    /// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>,
    /// which also determines up front whether the operation journals anything at all, this can only raise
    /// the level of an already-enabled operation - it never re-enables a disabled one. Can be called on the
    /// root operation or any nested sub-operation; either way it affects the one level the eventual entry
    /// gets written at, the same way <see cref="AddProperty{T}"/> affects the one set of properties.
    /// Typically called alongside <see cref="SetException"/>, but useful on its own too - e.g. a business
    /// failure that never throws (a rejected/backordered/declined result) can still warrant a higher level.
    /// </summary>
    /// <param name="level">The level to escalate to, if more severe than the operation's current level.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog Escalate(LogLevel level);

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
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog AddProperty<T>(string name, T value);

    /// <summary>
    /// Records <paramref name="value"/> as the result of this operation. On the root operation, this sets
    /// the <c>Operation.Result</c> structured property of the final log entry. On a sub-operation, this
    /// instead appends a "`Name` result: ..." line (rendered via
    /// <see cref="IFormattable"/>/<see cref="object.ToString"/>) to the journal - a sub-operation never gets
    /// its own structured property, since only the root ever writes a log entry. Typically called via the
    /// <see cref="OperationLogExtensions.RecordResultTo{T}"/> extension method rather than directly, so it
    /// can be chained onto a return expression.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog SetResult<T>(T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog Append(string text);

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
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

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
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Begins a nested sub-operation. A "started" line is immediately appended to the journal, and a
    /// "complete" line is appended when the returned <see cref="IOperationLog"/> is disposed. Unlike
    /// the root operation, a sub-operation never writes its own log entry - it only ever contributes to
    /// the root's single flushed entry.
    /// </summary>
    /// <param name="operationName">The sub-operation's name, used in its journal lines (e.g. "started"/"complete").</param>
    /// <returns>An <see cref="IOperationLog"/> representing the nested sub-operation.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    IOperationLog BeginSubOperation(string operationName);
}
