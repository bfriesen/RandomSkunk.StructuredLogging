using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging;

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
///         using var log = logger.LogOperation(LogLevel.Information, 1286859363, $"{typeof(Calculator)}.{nameof(Divide)}",
///             ("Operation.Dividend", dividend), ("Operation.Divisor", divisor), ("Operation.FallbackValue", fallbackValue));
/// 
///         if (!log.IsNull(fallbackValue) &amp;&amp; log.Condition(divisor == 0))
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
///             logger.Error(log.EventId, log.Exception(ex), "Error performing division. Rethrowing exception...", log.Properties);
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TNameValuePairList">A value type that implements IReadOnlyList&lt;KeyValuePair&lt;string, object?&gt;&gt;,
/// representing the operation's properties to be included in the log.</typeparam>
public struct OperationLog<TNameValuePairList> : IOperationLogInternal
    where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <summary>Has a non-null value only when logging is enabled for the operation.</summary>
    private StringBuilder? _stringBuilder;

    private TNameValuePairList _properties;
    private List<KeyValuePair<string, object?>>? _listProperties;
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
        string? operationCompleteMessage,
        TNameValuePairList properties)
    {
        // Always set the properties and event id fields.
        _properties = properties;
        _eventId = eventId;

        if (logger != null && operationCompleteMessage != null)
        {
            // Only set the rest of the fields if we know logging is enabled.
            _logger = logger;
            _logLevel = logLevel;
            _operationCompleteMessage = operationCompleteMessage;
            _stringBuilder = StringBuilderPool.Instance.Get();

            Append($"Operation started");
        }
    }

    /// <inheritdoc/>
    public readonly EventId EventId => _eventId;

    /// <summary>
    /// Gets the list of name value pairs associated with the operation.
    /// </summary>
    public List<KeyValuePair<string, object?>> Properties
    {
        get
        {
            if (_listProperties == null)
            {
                _listProperties = PropertyListPool.Instance.Get();
                _listProperties.AddRange(_properties);
            }

            return _listProperties;
        }
    }

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
        Append($"`{valueExpression}` is{(isNull ? null : " not")} null");
        return isNull;
    }

    /// <inheritdoc/>
    public bool IsNull<T>([NotNullWhen(false)] T? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
        where T : struct
    {
        bool isNull = !value.HasValue;
        Append($"`{valueExpression}` is{(isNull ? null : " not")} null");
        return isNull;
    }

    /// <inheritdoc/>
    public bool IsNullOrEmpty([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrEmpty = string.IsNullOrEmpty(value);
        Append($"`{valueExpression}` is{(isNullOrEmpty ? null : " not")} null or empty");
        return isNullOrEmpty;
    }

    /// <inheritdoc/>
    public bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value, [CallerArgumentExpression(nameof(value))] string valueExpression = null!)
    {
        bool isNullOrWhiteSpace = string.IsNullOrWhiteSpace(value);
        Append($"`{valueExpression}` is{(isNullOrWhiteSpace ? null : " not")} null or whitespace");
        return isNullOrWhiteSpace;
    }

    /// <summary>
    /// Logs the completion of the operation and releases all resources used by the current instance.
    /// </summary>
    public void Dispose()
    {
        if (_stringBuilder != null)
        {
            Append($"Operation complete");

            NameValuePairList2 additionalNameValuePairs = new();
            if (_hasReturnValue)
                additionalNameValuePairs.Add(new("ReturnValue", _returnValue));

            additionalNameValuePairs.Add(new("OperationLog", _stringBuilder.ToString()));

            MessageData message = new(_operationCompleteMessage, in additionalNameValuePairs);

            if (_listProperties != null)
            {
                _logger!.Log(
                    _logLevel,
                    _eventId,
                    new LogState<ReadOnlyNameValuePairList<List<KeyValuePair<string, object?>>>>(in message, new(_listProperties)),
                    _exception,
                    LogState<ReadOnlyNameValuePairList<List<KeyValuePair<string, object?>>>>.Formatter);

                PropertyListPool.Instance.Return(_listProperties);
            }
            else
            {
                _logger!.Log(
                    _logLevel,
                    _eventId,
                    new LogState<ReadOnlyNameValuePairList<TNameValuePairList>>(in message, new(_properties)),
                    _exception,
                    LogState<ReadOnlyNameValuePairList<TNameValuePairList>>.Formatter);
            }

            StringBuilderPool.Instance.Return(_stringBuilder);
        }

        this = default;
    }
}
