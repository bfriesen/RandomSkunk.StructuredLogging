using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging;

using static Operation;

/// <summary>
/// An object, that, when disposed, writes a structured log indicating that the operation is complete.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// public class Calculator(ILogger&lt;Calculator> logger)
/// {
///     public int Divide(int dividend, int divisor, int? fallbackValue = null)
///     {
///         using var op = logger.LogOperation(LogLevel.Information, 1286859363, $"{typeof(Calculator)}.{nameof(Divide)}", dividend, divisor, fallbackValue);
/// 
///         if (op.Condition(fallbackValue != null &amp;&amp; divisor == 0))
///         {
///             op.Log($"Cannot divide by zero. Returning fallback value, {fallbackValue}.");
///             return op.Return(fallbackValue!.Value);
///         }
/// 
///         try
///         {
///             return op.Return(dividend / divisor);
///         }
///         catch (Exception ex)
///         {
///             logger.Error(op.EventId, op.Exception(ex), "Error performing division. Rethrowing exception...", op.Parameters);
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TNameValuePairList">A value type that implements IReadOnlyList&lt;KeyValuePair&lt;string, object?&gt;&gt;,
/// representing the operation's parameters to be included in the log.</typeparam>
public struct Operation<TNameValuePairList> : IDisposable
    where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>Has a non-null value only when logging is enabled for the operation.</summary>
    internal StringBuilder? _operationLog;

    private TNameValuePairList _operationParameters;
    private ILogger? _logger;
    private LogLevel _logLevel;
    private EventId _eventId;
    private Exception? _exception;

    private string? _operationCompleteMessage;

    private object? _returnValue;
    private bool _hasReturnValue;

    internal Operation(
        ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        TNameValuePairList operationParameters)
    {
        // Always set the operation parameters and event id fields.
        _operationParameters = operationParameters;
        _eventId = eventId;

        if (logger != null && operationName != null)
        {
            MessageData message = new($"Operation starting: {operationName}");
            logger.Log(
                logLevel,
                eventId,
                new LogState<TNameValuePairList>(in message, operationParameters),
                null,
                LogState<TNameValuePairList>.Formatter);

            // Only set the rest of the fields if we know logging is enabled.
            _logger = logger;
            _logLevel = logLevel;
            _operationCompleteMessage = $"Operation complete: {operationName}";
            _operationLog = _stringBuilderPool.Get();
        }
    }

    /// <summary>
    /// Gets the collection of parameters associated with the operation.
    /// </summary>
    public readonly TNameValuePairList Parameters => _operationParameters;

    /// <summary>
    /// Gets the event id associated with the operation.
    /// </summary>
    public readonly EventId EventId => _eventId;

    /// <summary>
    /// Adds the specified log entry to the operation log.
    /// </summary>
    /// <param name="logEntry">The log entry to add to the operation log.</param>
    public readonly void Log(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedString.OperationLogEntry<TNameValuePairList> logEntry) =>
        _operationLog?.Append(ref logEntry._innerHandler);

    /// <summary>
    /// Sets the <c>ReturnValue</c> property of the operation complete log and returns the same value.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="returnValue">The return value of the operation.</param>
    /// <returns>The same return value.</returns>
    [return: NotNullIfNotNull(nameof(returnValue))]
    public T? Return<T>(T? returnValue)
    {
        if (_operationLog != null)
        {
            _returnValue = returnValue;
            _hasReturnValue = true;
            Log($"Return value of type {returnValue?.GetType() ?? typeof(T)} set.");
        }

        return returnValue;
    }

    /// <summary>
    /// Sets the exception of the operation complete log and returns the same exception.
    /// </summary>
    /// <typeparam name="TException">The type of exception to set and return. Must derive from Exception.</typeparam>
    /// <param name="exception">The exception instance to set as the current error.</param>
    /// <returns>The same exception instance provided in the exception parameter.</returns>
    [return: NotNullIfNotNull(nameof(exception))]
    public TException? Exception<TException>(TException? exception)
        where TException : Exception
    {
        if (_operationLog != null)
        {
            _exception = exception;
            Log($"Exception of type {exception?.GetType() ?? typeof(TException)} set.");
        }

        return exception;
    }

    /// <summary>
    /// Adds a log entry containing the specified boolean condition and its original expression to the completion log's <c>OperationLog</c> property, then
    /// returns the same value.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] (divisor == 0): true" to the operation log:
    /// <code>
    /// if (op.Log(divisor == 0))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="condition">The boolean condition to log and return.</param>
    /// <param name="conditionExpression">The string representation of the original expression passed as the condition. This is
    /// automatically provided by the compiler and should not be set manually.</param>
    /// <returns>The <paramref name="condition"/> parameter.</returns>
    public readonly bool Condition(bool condition, [CallerArgumentExpression(nameof(condition))] string conditionExpression = null!)
    {
        Log($"({conditionExpression}): {(condition ? "true" : "false")}");
        return condition;
    }

    /// <summary>
    /// Adds a log entry indicating whether the specified object is null to the completion log's <c>OperationLog</c> property,
    /// then returns whether the specified object is null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] (myValue is null): true" to the operation log:
    /// <code>
    /// if (op.IsNull(myValue))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is null; otherwise, false.</returns>
    public bool IsNull<T>([NotNullWhen(false)] T value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : class?
    {
        bool isNull = value is null;
        Log($"({valueExpression} is null): {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <summary>
    /// Adds a log entry indicating whether the specified object is null to the completion log's <c>OperationLog</c> property,
    /// then returns whether the specified object is null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] (myValue is null): true" to the operation log:
    /// <code>
    /// if (op.IsNull(myValue))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is null; otherwise, false.</returns>
    public bool IsNull<T>([NotNullWhen(false)] T? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : struct
    {
        bool isNull = value is null;
        Log($"({valueExpression} is null): {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <summary>
    /// Adds a log entry indicating whether the specified string is null or empty to the completion log's <c>OperationLog</c> property,
    /// then returns whether the specified string is null or empty.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] string.IsNullOrEmpty(myValue): true" to the operation log:
    /// <code>
    /// if (op.IsNullOrEmpty(myValue))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is null; otherwise, false.</returns>
    public bool IsNullOrEmpty([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrEmpty = string.IsNullOrEmpty(value);
        Log($"string.IsNullOrEmpty({valueExpression}): {(isNullOrEmpty ? "true" : "false")}");
        return isNullOrEmpty;
    }

    /// <summary>
    /// Adds a log entry indicating whether the specified string is null or whitespace to the completion log's <c>OperationLog</c> property,
    /// then returns whether the specified string is null or whitespace.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] string.IsNullOrWhiteSpace(myValue): true" to the operation log:
    /// <code>
    /// if (op.IsNullOrWhiteSpace(myValue))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is null; otherwise, false.</returns>
    public bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrWhiteSpace = string.IsNullOrWhiteSpace(value);
        Log($"string.IsNullOrWhiteSpace({valueExpression}): {(isNullOrWhiteSpace ? "true" : "false")}");
        return isNullOrWhiteSpace;
    }

    /// <summary>
    /// Logs the completion of the operation and releases all resources used by the current instance.
    /// </summary>
    public void Dispose()
    {
        if (_operationLog != null)
        {
            NameValuePairList2 additionalNameValuePairs = new();
            if (_hasReturnValue)
                additionalNameValuePairs.Add(new("ReturnValue", _returnValue));
            if (_operationLog.Length > 0)
                additionalNameValuePairs.Add(new("OperationLog", _operationLog.ToString()));

            MessageData message = new(_operationCompleteMessage, in additionalNameValuePairs);
            _logger!.Log(
                _logLevel,
                _eventId,
                new LogState<TNameValuePairList>(in message, _operationParameters),
                _exception,
                LogState<TNameValuePairList>.Formatter);

            _stringBuilderPool.Return(_operationLog);
        }

        this = default;
    }
}

// This class exists because the public Operation struct is generic and would have a copy of the object pool for each closed
// generic type. We want something non-generic that can be shared among all instances of all closed generic types.
internal static class Operation
{
    internal static readonly ObjectPool<StringBuilder> _stringBuilderPool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
}
