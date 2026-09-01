using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Internal counterpart to <see cref="ISubOperationLog"/>: declares the same members, but every member
/// whose <see cref="ISubOperationLog"/> counterpart is fluent (returns <see cref="ISubOperationLog"/>
/// itself) returns <see langword="void"/> here instead. Implemented explicitly by
/// <see cref="OperationLogBase"/> and <see cref="SynchronizedOperationLogBase"/> - the two internal base
/// classes shared by <see cref="IOperationLog"/>'s and <see cref="ISubOperationLog"/>'s concrete
/// implementations - so that each can provide one shared, <see langword="void"/>-returning implementation
/// of a member without colliding with the self-returning member of the same name that
/// <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> (or
/// <see cref="SynchronizedOperationLog"/>/<see cref="SynchronizedSubOperationLog"/>) declares to actually
/// satisfy <see cref="IOperationLog"/>/<see cref="ISubOperationLog"/>. Not implemented by
/// <see cref="IOperationLog"/>/<see cref="ISubOperationLog"/> themselves, and not part of the public API -
/// unlike the public, generic type of the same name this replaced, this one exists purely as
/// implementation-sharing plumbing between the two base classes.
/// </summary>
public interface IOperationLogBase : IDisposable
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
    /// Typically called alongside <see cref="AppendException"/> (or <see cref="IOperationLog.SetException"/>), but useful
    /// on its own too - e.g. a business failure that never throws (a rejected/backordered/declined result)
    /// can still warrant a higher level.
    /// </summary>
    /// <param name="level">The level to escalate to, if more severe than the operation's current level.</param>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void Escalate(LogLevel level);

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
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void AddProperty<T>(string name, T value);

    /// <summary>
    /// Appends a "Operation failed: ..." line describing <paramref name="exception"/> to the journal.
    /// Unlike <see cref="IOperationLog.SetException"/>, this never sets the <c>Exception</c> argument of the final log
    /// entry - only <see cref="IOperationLog.SetException"/> can do that. This does not, by itself, change the level the
    /// final log entry is written at - call <see cref="Escalate"/> as well if the exception should also
    /// raise the operation's level.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void AppendException(Exception exception);

    /// <summary>
    /// Appends a "Operation result: ..." line (rendered via <see cref="IFormattable"/>/<see cref="object.ToString"/>)
    /// describing <paramref name="value"/> to the journal. Unlike <see cref="IOperationLog.SetResult{T}"/>, this never
    /// sets the <c>Operation.Result</c> structured property of the final log entry - only
    /// <see cref="IOperationLog.SetResult{T}"/> can do that. Typically called via the
    /// <see cref="RandomSkunk.StructuredLogging.Operation.Fluent.OperationLogExtensions.AppendResultTo{T}(T, IOperationLog)"/> extension method rather than
    /// directly, so it can be chained onto a return expression.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">The result to record.</param>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void AppendResult<T>(T value);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void Append(string text);

    /// <summary>
    /// Appends a line of free text to the operation's journal, which becomes the message of the
    /// final log entry. Unlike <see cref="Append(string)"/>, <paramref name="text"/>'s interpolated
    /// arguments are only evaluated if <see cref="IOperationLogBase.IsEnabled"/> is <see langword="true"/> - see
    /// <see cref="OperationLogInterpolatedStringHandler"/>.
    /// </summary>
    /// <param name="text">The text to append.</param>
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void Append([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler text);

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
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    void AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

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
    /// <exception cref="ObjectDisposedException">The operation has already been disposed.</exception>
    /// <remarks>
    /// This is the one member of this interface that isn't trimming/Native AOT safe: it serializes
    /// <paramref name="value"/> with reflection-based <see cref="System.Text.Json.JsonSerializer"/>, so it is
    /// annotated <see cref="RequiresUnreferencedCodeAttribute"/>/<see cref="RequiresDynamicCodeAttribute"/> and
    /// warns at the call site in a trimmed or AOT-published application. Use <see cref="AppendValue{T}"/> there
    /// instead, or preserve the serialized type.
    /// </remarks>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    void AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null);

    /// <summary>
    /// Begins a nested sub-operation. A "started" line is immediately appended to the journal, and a
    /// "complete" line is appended when the returned <see cref="ISubOperationLog"/> is disposed. Like this
    /// operation, the returned sub-operation never writes its own log entry - it only ever contributes to
    /// the root's single flushed entry.
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
