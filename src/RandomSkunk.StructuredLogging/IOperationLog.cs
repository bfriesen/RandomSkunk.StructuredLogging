using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Represents an in-progress operation being journaled by
/// <see cref="LoggerOperationExtensions.BeginOperation(Microsoft.Extensions.Logging.ILogger, string, Microsoft.Extensions.Logging.LogLevel, bool)"/>.
/// Disposing the root <see cref="IOperationLog"/> writes exactly one log entry summarizing everything that
/// happened during the operation, including any nested <see cref="ISubOperationLog"/> activity.
/// </summary>
public interface IOperationLog : IDisposable
{
    /// <summary>
    /// Adds a structured property to the operation's final log entry. Unlike the built-in
    /// <c>Operation.*</c> properties (<c>Operation.StartTime</c>, <c>Operation.DurationMs</c>,
    /// <c>Operation.Log</c>, <c>Operation.Result</c>), properties set here are added unprefixed. Can be
    /// called on the root operation or any nested
    /// sub-operation; either way the property is added to the one entry that eventually gets flushed.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    IOperationLog SetProperty<T>(string name, T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal (the <c>Operation.Log</c> property of the
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
    /// Begins a nested sub-operation. A "started" line is immediately appended to the journal, and a
    /// "complete" line is appended when the returned <see cref="ISubOperationLog"/> is disposed. Unlike
    /// the root operation, a sub-operation never writes its own log entry - it only ever contributes to
    /// the root's single flushed entry.
    /// </summary>
    /// <param name="name">The sub-operation's name, used in its journal lines (e.g. "started"/"complete").</param>
    /// <returns>An <see cref="ISubOperationLog"/> representing the nested sub-operation.</returns>
    ISubOperationLog BeginSubOperation(string name);

    /// <summary>
    /// Records the exception for this operation. On the root operation, this becomes the <c>Exception</c>
    /// argument of the final log entry. See <see cref="ISubOperationLog.SetException(Exception, bool)"/> for
    /// the corresponding sub-operation behavior, including whether it propagates to the root.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    IOperationLog SetException(Exception exception);

    /// <summary>
    /// Records <paramref name="value"/> as the result of this operation. On the root operation, this sets
    /// the <c>Operation.Result</c> structured property of the final log entry. See
    /// <see cref="ISubOperationLog.SetResult{T}"/> for the corresponding sub-operation behavior. Typically
    /// called via the <see cref="OperationLogExtensions.OperationLogSetResult{T}"/> extension method rather
    /// than directly, so it can be chained onto a return expression.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <see cref="IOperationLog"/>, so calls can be chained.</returns>
    IOperationLog SetResult<T>(T value);
}
