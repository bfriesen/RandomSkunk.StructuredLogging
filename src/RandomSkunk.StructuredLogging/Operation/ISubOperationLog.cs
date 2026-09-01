using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation, returned by <see cref="IOperationLogBase.BeginSubOperation(string)"/> on either the
/// root operation or another sub-operation. Every fluent member returns <see cref="ISubOperationLog"/>
/// itself, so a chain of calls on a sub-operation keeps returning a sub-operation - deliberately unrelated
/// to <see cref="IOperationLog"/> (neither extends the other; each declares its own copy of the members
/// they share), so <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/> can
/// never be reached from a sub-operation reference; use <see cref="AppendException"/>/
/// <see cref="AppendResult{T}"/> instead. A sub-operation never writes its own log entry - it only ever
/// contributes to the root's one eventual log entry. Every sub-operation shares the root operation's
/// journal, so once the root <see cref="IOperationLog"/> has been disposed, calling any member other than
/// <c>Properties</c>, <c>EventId</c>, or <c>IsEnabled</c> on a sub-operation still referenced from outside
/// the root's <c>using</c> scope throws <see cref="ObjectDisposedException"/>, rather than risking
/// corruption of an unrelated operation's journal. That includes <see cref="IDisposable.Dispose"/>: it's
/// only safe to call once per sub-operation - disposing one after the root is already disposed throws like
/// everything else.
/// </summary>
public interface ISubOperationLog : IOperationLogBase
{
    /// <summary>
    /// Raises the level the operation's final log entry is written at, if <paramref name="level"/> is more
    /// severe than the operation's current level - otherwise this is a no-op. Unlike the level passed to
    /// <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>,
    /// which also determines up front whether the operation journals anything at all, this can only raise
    /// the level of an already-enabled operation - it never re-enables a disabled one. Can be called on the
    /// root operation or any nested sub-operation; either way it affects the one level the eventual entry
    /// gets written at, the same way <see cref="AddProperty{T}"/> affects the one set of properties.
    /// Typically called alongside <see cref="AppendException"/>, but useful on its own too - e.g. a
    /// business failure that never throws (a rejected/backordered/declined result) can still warrant a
    /// higher level.
    /// </summary>
    /// <param name="level">The level to escalate to, if more severe than the operation's current level.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog Escalate(LogLevel level);

    /// <summary>
    /// Adds a structured property to the operation's final log entry. Unlike the built-in
    /// <c>Operation.*</c> properties (<c>Operation.Name</c>, <c>Operation.StartTime</c>,
    /// <c>Operation.DurationSeconds</c>, <c>Operation.Result</c>), properties set here are added unprefixed.
    /// Can be called on the root operation or any nested
    /// sub-operation; either way the property is added to the one entry that eventually gets flushed.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog AddProperty<T>(string propertyName, T value);

    /// <summary>
    /// Appends a "`name` failed: ..." line describing <paramref name="exception"/> to the journal. Unlike
    /// <see cref="IOperationLog.SetException"/>, this never sets the <c>Exception</c> argument of the final
    /// log entry - only the root's own <see cref="IOperationLog.SetException"/> can do that. This does not,
    /// by itself, change the level the final log entry is written at - call <see cref="Escalate"/> as well
    /// if the exception should also raise the operation's level.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog AppendException(Exception exception);

    /// <summary>
    /// Appends a "`name` result: ..." line (rendered via <see cref="IFormattable"/>/<see cref="object.ToString"/>)
    /// describing <paramref name="value"/> to the journal. Unlike <see cref="IOperationLog.SetResult{T}"/>,
    /// this never sets the <c>Operation.Result</c> structured property of the final log entry - only the
    /// root's own <see cref="IOperationLog.SetResult{T}"/> can do that. Typically called via the
    /// <see cref="Fluent.OperationLogExtensions.AppendResultTo{T}(T, ISubOperationLog)"/> extension method
    /// rather than directly, so it can be chained onto a return expression.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog AppendResult<T>(T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog Append(string text);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry. Unlike <see cref="Append(string)"/>, <paramref name="text"/>'s interpolated
    /// arguments are only evaluated if <see cref="IOperationLogBase.IsEnabled"/> is <see langword="true"/> - see
    /// <see cref="OperationLogInterpolatedStringHandler"/>.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog Append([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler text);

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
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    new ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

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
    /// <returns>This <see cref="ISubOperationLog"/>, so calls can be chained.</returns>
    /// <exception cref="ObjectDisposedException">The root operation has already been disposed.</exception>
    /// <remarks>
    /// This is the one member of this interface that isn't trimming/Native AOT safe: it serializes
    /// <paramref name="value"/> with reflection-based <see cref="System.Text.Json.JsonSerializer"/>, so it is
    /// annotated <see cref="RequiresUnreferencedCodeAttribute"/>/<see cref="RequiresDynamicCodeAttribute"/> and
    /// warns at the call site in a trimmed or AOT-published application. Use <see cref="AppendValue{T}"/> there
    /// instead, or preserve the serialized type.
    /// </remarks>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    new ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);
}
