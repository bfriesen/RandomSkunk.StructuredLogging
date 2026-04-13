using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging;

using static OperationLog;

/// <summary>
/// Represents an operation log. When disposed, writes a structured log indicating that the operation is complete.
/// </summary>
/// <remarks>
/// Example:
/// <code>
/// public class Calculator(ILogger&lt;Calculator> logger)
/// {
///     public int Divide(int dividend, int divisor, int? fallbackValue = null)
///     {
///         using var log = logger.LogOperation(LogLevel.Information, 1286859363, $"{typeof(Calculator)}.{nameof(Divide)}", dividend, divisor, fallbackValue);
/// 
///         if (log.IsNotNull(fallbackValue) &amp;&amp; log.Condition(divisor == 0))
///         {
///             log.Append($"Cannot divide by zero. Returning fallback value, {fallbackValue}.");
///             return log.ReturnValue(fallbackValue.Value);
///         }
/// 
///         try
///         {
///             return log.ReturnValue(dividend / divisor);
///         }
///         catch (Exception ex)
///         {
///             logger.Error(log.EventId, log.Exception(ex), "Error performing division. Rethrowing exception...", log.Parameters);
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TNameValuePairList">A value type that implements IReadOnlyList&lt;KeyValuePair&lt;string, object?&gt;&gt;,
/// representing the operation's parameters to be included in the log.</typeparam>
public struct OperationLog<TNameValuePairList> : IDisposable
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

    internal OperationLog(
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
    /// Gets the collection of parameters associated with the operation log.
    /// </summary>
    public readonly TNameValuePairList Parameters => _operationParameters;

    /// <summary>
    /// Gets the event id associated with the operation log.
    /// </summary>
    public readonly EventId EventId => _eventId;

    /// <summary>
    /// Appends the interpolated log entry string to the operation log.
    /// </summary>
    /// <param name="logEntry">The log entry to add to the operation log.</param>
    public readonly void Append(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedString.OperationLogEntry<TNameValuePairList> logEntry) =>
        _operationLog?.Append(ref logEntry._innerHandler);

    /// <summary>
    /// Sets the <c>ReturnValue</c> property of the operation complete log and returns the same value.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="returnValue">The return value of the operation.</param>
    /// <param name="returnValueExpression">The string representation of the original expression passed as the return value. This
    /// is automatically provided by the compiler and should not be set manually.</param>
    /// <returns>The same return value.</returns>
    public T ReturnValue<T>(
        T returnValue,
        [CallerArgumentExpression(nameof(returnValue))] string returnValueExpression = null!)
    {
        if (_operationLog != null)
        {
            _returnValue = returnValue;
            _hasReturnValue = true;
            Append($"Return value is `{returnValueExpression}`");
        }

        return returnValue;
    }

    /// <summary>
    /// Sets the exception of the operation complete log and returns the same exception.
    /// </summary>
    /// <typeparam name="TException">The type of exception to set and return. Must derive from Exception.</typeparam>
    /// <param name="exception">The exception instance to set as the current error.</param>
    /// <param name="exceptionExpression">The string representation of the original expression passed as the exception. This is
    /// automatically provided by the compiler and should not be set manually.</param>
    /// <returns>The same exception instance provided in the exception parameter.</returns>
    public TException Exception<TException>(
        TException exception,
        [CallerArgumentExpression(nameof(exception))] string exceptionExpression = null!)
        where TException : Exception
    {
        if (_operationLog != null)
        {
            _exception = exception;
            Append($"Exception is `{typeof(TException).Name} {exceptionExpression}`");
        }

        return exception;
    }

    /// <summary>
    /// Adds a log entry containing the specified boolean condition and its original expression to the completion log's <c>OperationLog</c> property, then
    /// returns the same value.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `divisor == 0` is true" to the operation log:
    /// <code>
    /// if (log.Condition(divisor == 0))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="condition">The boolean condition to log and return.</param>
    /// <param name="conditionExpression">The string representation of the original expression passed as the condition. This is
    /// automatically provided by the compiler and should not be set manually.</param>
    /// <returns>The <paramref name="condition"/> parameter.</returns>
    public readonly bool Condition(
        bool condition,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = null!)
    {
        Append($"`{conditionExpression}` is {(condition ? "true" : "false")}");
        return condition;
    }

    /// <summary>
    /// Returns whether the specified value is null. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `firstName == null` is true" to the operation
    /// log:
    /// <code>
    /// if (log.IsNull(firstName))
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
        bool isNull = value == null;
        Append($"`{valueExpression} == null` is {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <summary>
    /// Returns whether the specified value is null. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `monthlyIncome == null` is true" to the
    /// operation log:
    /// <code>
    /// if (log.IsNull(monthlyIncome))
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
        bool isNull = !value.HasValue;
        Append($"`{valueExpression} == null` is {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <summary>
    /// Returns whether the specified value is null or empty. If logging is enabled for the operation, also appends a log entry
    /// to the operation log that indicates whether the value is null or empty.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `string.IsNullOrEmpty(firstName)` is true" to
    /// the operation log:
    /// <code>
    /// if (log.IsNullOrEmpty(firstName))
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
        Append($"`string.IsNullOrEmpty({valueExpression})` is {(isNullOrEmpty ? "true" : "false")}");
        return isNullOrEmpty;
    }

    /// <summary>
    /// Returns whether the specified value is null or whitespace. If logging is enabled for the operation, also appends a log entry
    /// to the operation log that indicates whether the value is null or whitespace.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `string.IsNullOrWhiteSpace(firstName)` is true" to
    /// the operation log:
    /// <code>
    /// if (log.IsNullOrWhiteSpace(firstName))
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
        Append($"`string.IsNullOrWhiteSpace({valueExpression})` is {(isNullOrWhiteSpace ? "true" : "false")}");
        return isNullOrWhiteSpace;
    }

    /// <summary>
    /// Returns whether the specified value is not null. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is not null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `firstName != null` is true" to the operation
    /// log:
    /// <code>
    /// if (log.IsNotNull(firstName))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is not null; otherwise, false.</returns>
    public bool IsNotNull<T>([NotNullWhen(true)] T value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : class?
    {
        bool isNotNull = value != null;
        Append($"`{valueExpression} != null` is {(isNotNull ? "true" : "false")}");
        return isNotNull;
    }

    /// <summary>
    /// Returns whether the specified value is not null. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is not null.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `monthlyIncome != null` is true" to the operation
    /// log:
    /// <code>
    /// if (log.IsNotNull(monthlyIncome))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is not null; otherwise, false.</returns>
    public bool IsNotNull<T>([NotNullWhen(true)] T? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : struct
    {
        bool isNotNull = value.HasValue;
        Append($"`{valueExpression} != null` is {(isNotNull ? "true" : "false")}");
        return isNotNull;
    }

    /// <summary>
    /// Returns whether the specified value is not null or empty. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is not null or empty.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `!string.IsNullOrEmpty(firstName)` is true" to the
    /// operation log:
    /// <code>
    /// if (log.IsNotNullOrEmpty(firstName))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is not null; otherwise, false.</returns>
    public bool IsNotNullOrEmpty([NotNullWhen(true)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNotNullOrEmpty = !string.IsNullOrEmpty(value);
        Append($"`!string.IsNullOrEmpty({valueExpression})` is {(isNotNullOrEmpty ? "true" : "false")}");
        return isNotNullOrEmpty;
    }

    /// <summary>
    /// Returns whether the specified value is not null or whitespace. If logging is enabled for the operation, also appends a log entry to the
    /// operation log that indicates whether the value is not null or whitespace.
    /// </summary>
    /// <remarks>The following example adds an entry similar to "[20:57:24.615Z] `!string.IsNullOrWhiteSpace(firstName)` is true" to
    /// the operation log:
    /// <code>
    /// if (log.IsNotNullOrWhiteSpace(firstName))
    ///     ...
    /// </code>
    /// </remarks>
    /// <param name="value">The object to check.</param>
    /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
    /// compiler.</param>
    /// <returns>true if the specified object is not null; otherwise, false.</returns>
    public bool IsNotNullOrWhiteSpace([NotNullWhen(true)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNotNullOrWhiteSpace = !string.IsNullOrWhiteSpace(value);
        Append($"`!string.IsNullOrWhiteSpace({valueExpression})` is {(isNotNullOrWhiteSpace ? "true" : "false")}");
        return isNotNullOrWhiteSpace;
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
internal static class OperationLog
{
    internal static readonly ObjectPool<StringBuilder> _stringBuilderPool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
}
