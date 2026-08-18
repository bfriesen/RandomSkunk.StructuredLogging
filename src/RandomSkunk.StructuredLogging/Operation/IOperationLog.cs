using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Represents an in-progress operation (or nested sub-operation) being journaled by
/// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>.
/// Disposing the root <see cref="IOperationLog"/> writes exactly one log entry summarizing everything that
/// happened during the operation, including any nested sub-operation activity. A sub-operation, returned by
/// <see cref="BeginSubOperation"/>, never writes its own log entry - disposing it appends a "complete" line
/// to the ancestor journal it was created from instead.
/// </summary>
public interface IOperationLog : IDisposable
{
    /// <summary>
    /// Records the exception for this operation. On the root operation, this becomes the <c>Exception</c>
    /// argument of the final log entry, and <paramref name="recordEverywhere"/> additionally appends a
    /// "failed" line to the journal describing it. On a sub-operation, a "failed" line is always appended
    /// to the journal, and <paramref name="recordEverywhere"/> additionally sets this exception as the root
    /// operation's <c>Exception</c> (used in the final log entry, e.g. for backend stack-trace/exception
    /// indexing).
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="recordEverywhere">
    /// On the root operation, <see langword="true"/> to also append a "failed" line to the journal (the
    /// exception always becomes the final log entry's <c>Exception</c> argument regardless). On a
    /// sub-operation, <see langword="true"/> to also set this exception as the root operation's exception
    /// (last call at any level wins; the exception always gets a "failed" journal line regardless).
    /// <see langword="false"/> to record it only in the location that's implicit for this operation level.
    /// </param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    IOperationLog SetException(Exception exception, bool recordEverywhere = false);

    /// <summary>
    /// Adds a structured property to the operation's final log entry. Unlike the built-in
    /// <c>Operation.*</c> properties (<c>Operation.StartTime</c>, <c>Operation.DurationSeconds</c>,
    /// <c>Operation.Journal</c>, <c>Operation.Result</c>), properties set here are added unprefixed. Can be
    /// called on the root operation or any nested
    /// sub-operation; either way the property is added to the one entry that eventually gets flushed.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
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
    IOperationLog SetResult<T>(T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal (the <c>Operation.Journal</c> property of the
    /// final log entry).
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
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
    IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Begins a nested sub-operation. A "started" line is immediately appended to the journal, and a
    /// "complete" line is appended when the returned <see cref="IOperationLog"/> is disposed. Unlike
    /// the root operation, a sub-operation never writes its own log entry - it only ever contributes to
    /// the root's single flushed entry.
    /// </summary>
    /// <param name="name">The sub-operation's name, used in its journal lines (e.g. "started"/"complete").</param>
    /// <returns>An <see cref="IOperationLog"/> representing the nested sub-operation.</returns>
    IOperationLog BeginSubOperation(string name);
}
