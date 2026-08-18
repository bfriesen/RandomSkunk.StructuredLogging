using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Represents a nested sub-operation begun via <see cref="IOperationLog.BeginSubOperation"/>. Disposing a
/// sub-operation appends a "complete" line to the ancestor journal it was created from - it never writes
/// its own log entry.
/// </summary>
public interface ISubOperationLog : IOperationLog
{
    /// <summary>
    /// Adds a structured property to the operation's final log entry. See <see cref="IOperationLog.SetProperty{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog SetProperty<T>(string name, T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal. See <see cref="IOperationLog.Append"/>.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog Append(string text);

    /// <summary>
    /// Appends a line of free text to the operation's journal in the form <c>`valueName`: value</c>. See
    /// <see cref="IOperationLog.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Appends a line of free text to the operation's journal in the form <c>`valueName`: value</c>,
    /// rendering <paramref name="value"/> as indented JSON. See <see cref="IOperationLog.AppendJson{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Records the exception for this sub-operation, appending a "failed" line to the journal. See
    /// <see cref="IOperationLog.SetException(Exception)"/>.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog SetException(Exception exception);

    /// <summary>
    /// Records the exception for this sub-operation, appending a "failed" line to the journal, and
    /// explicitly chooses whether that exception also becomes the root operation's <c>Exception</c>
    /// (used in the final log entry, e.g. for backend stack-trace/exception indexing).
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="propagateToRoot">
    /// <see langword="true"/> to also set this exception as the root operation's exception (last call at
    /// any level wins); <see langword="false"/> to record it only in this sub-operation's journal line.
    /// </param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    ISubOperationLog SetException(Exception exception, bool propagateToRoot);

    /// <summary>
    /// Records <paramref name="value"/> as the result of this sub-operation, appending a
    /// "`Name` result: ..." line (rendered via <see cref="IFormattable"/>/<see cref="object.ToString"/>) to
    /// the journal. Unlike the root operation, a sub-operation never gets its own structured property,
    /// since only the root ever writes a log entry. See <see cref="IOperationLog.SetResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    new ISubOperationLog SetResult<T>(T value);
}
