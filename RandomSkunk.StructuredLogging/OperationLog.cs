using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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
public struct OperationLog<TNameValuePairList> : IOperationLogInternal
    where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>Has a non-null value only when logging is enabled for the operation.</summary>
    private StringBuilder? _stringBuilder;

    private TNameValuePairList _parameters;
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
        _parameters = operationParameters;
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
            _stringBuilder = _stringBuilderPool.Get();
        }
    }

    /// <summary>
    /// Gets the collection of parameters associated with the operation log.
    /// </summary>
    public readonly TNameValuePairList Parameters => _parameters;

    /// <inheritdoc/>
    public readonly EventId EventId => _eventId;

    readonly IReadOnlyList<KeyValuePair<string, object?>> IOperationLog.Parameters => Parameters;

    /// <summary>
    /// Gets the string builder for the operation log. Has a non-null value only when logging is enabled for the operation.
    /// </summary>
    internal readonly StringBuilder? StringBuilder => _stringBuilder;

    readonly StringBuilder? IOperationLogInternal.StringBuilder => _stringBuilder;

    /// <summary>
    /// Appends the interpolated log entry string to the operation log.
    /// </summary>
    /// <param name="logEntry">The log entry to add to the operation log.</param>
    public readonly void Append(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedString.OperationLogEntry<TNameValuePairList> logEntry) =>
        _stringBuilder?.Append(ref logEntry._innerHandler);

    readonly void IOperationLog.Append(
        [InterpolatedStringHandlerArgument("")]
        ref InterpolatedString.OperationLogEntry logEntry) =>
        _stringBuilder?.Append(ref logEntry._innerHandler);

    /// <inheritdoc/>
    public T ReturnValue<T>(
        T returnValue,
        [CallerArgumentExpression(nameof(returnValue))] string returnValueExpression = null!)
    {
        if (_stringBuilder != null)
        {
            _returnValue = returnValue;
            _hasReturnValue = true;
            Append($"Return value set to `{returnValueExpression}`");
        }

        return returnValue;
    }

    /// <inheritdoc/>
    public TException Exception<TException>(
        TException exception,
        [CallerArgumentExpression(nameof(exception))] string exceptionExpression = null!)
        where TException : Exception
    {
        if (_stringBuilder != null)
        {
            _exception = exception;
            Append($"Exception set to `{typeof(TException).Name} {exceptionExpression}`");
        }

        return exception;
    }

    /// <inheritdoc/>
    public readonly T Value<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        Append($"`{valueExpression}` is {value?.ToString() ?? "null"}");
        return value;
    }

    /// <inheritdoc/>
    public readonly T JsonValue<T>(
        T value,
        JsonSerializerOptions? options = null,
        [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        Append($"`{valueExpression}` is {JsonSerializer.Serialize(value, options)}");
        return value;
    }

    /// <inheritdoc/>
    public readonly bool Condition(
        bool condition,
        [CallerArgumentExpression(nameof(condition))] string conditionExpression = null!)
    {
        Append($"`{conditionExpression}` is {(condition ? "true" : "false")}");
        return condition;
    }

    /// <inheritdoc/>
    public bool IsNull<T>([NotNullWhen(false)] T value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : class?
    {
        bool isNull = value is null;
        Append($"`{valueExpression} == null` is {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <inheritdoc/>
    public bool IsNull<T>([NotNullWhen(false)] T? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : struct
    {
        bool isNull = !value.HasValue;
        Append($"`{valueExpression} == null` is {(isNull ? "true" : "false")}");
        return isNull;
    }

    /// <inheritdoc/>
    public bool IsNullOrEmpty([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrEmpty = string.IsNullOrEmpty(value);
        Append($"`string.IsNullOrEmpty({valueExpression})` is {(isNullOrEmpty ? "true" : "false")}");
        return isNullOrEmpty;
    }

    /// <inheritdoc/>
    public bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrWhiteSpace = string.IsNullOrWhiteSpace(value);
        Append($"`string.IsNullOrWhiteSpace({valueExpression})` is {(isNullOrWhiteSpace ? "true" : "false")}");
        return isNullOrWhiteSpace;
    }

    /// <inheritdoc/>
    public bool IsNotNull<T>([NotNullWhen(true)] T value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : class?
    {
        bool isNotNull = value is not null;
        Append($"`{valueExpression} != null` is {(isNotNull ? "true" : "false")}");
        return isNotNull;
    }

    /// <inheritdoc/>
    public bool IsNotNull<T>([NotNullWhen(true)] T? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : struct
    {
        bool isNotNull = value.HasValue;
        Append($"`{valueExpression} != null` is {(isNotNull ? "true" : "false")}");
        return isNotNull;
    }

    /// <inheritdoc/>
    public bool IsNotNullOrEmpty([NotNullWhen(true)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNotNullOrEmpty = !string.IsNullOrEmpty(value);
        Append($"`!string.IsNullOrEmpty({valueExpression})` is {(isNotNullOrEmpty ? "true" : "false")}");
        return isNotNullOrEmpty;
    }

    /// <inheritdoc/>
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
        if (_stringBuilder != null)
        {
            NameValuePairList2 additionalNameValuePairs = new();
            if (_hasReturnValue)
                additionalNameValuePairs.Add(new("ReturnValue", _returnValue));
            if (_stringBuilder.Length > 0)
                additionalNameValuePairs.Add(new("OperationLog", _stringBuilder.ToString()));

            MessageData message = new(_operationCompleteMessage, in additionalNameValuePairs);
            _logger!.Log(
                _logLevel,
                _eventId,
                new LogState<TNameValuePairList>(in message, _parameters),
                _exception,
                LogState<TNameValuePairList>.Formatter);

            _stringBuilderPool.Return(_stringBuilder);
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
