using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
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
    where TNameValuePairList : IReadOnlyList<KeyValuePair<string, object?>>
{
    private delegate TNameValuePairList GetPropertiesDelegate(ref OperationLog<TNameValuePairList> log);
    private delegate void LogDelegate(ILogger logger, LogLevel logLevel, EventId eventId, ref readonly MessageData messageData, Exception? exception, TNameValuePairList properties);

    private static readonly GetPropertiesDelegate _propertiesGetter;
    private static readonly LogDelegate _log;

    /// <summary>Has a non-null value only when logging is enabled for the operation.</summary>
    private StringBuilder? _stringBuilder;

    private TNameValuePairList _properties;
    private ILogger? _logger;
    private LogLevel _logLevel;
    private EventId _eventId;
    private Exception? _exception;

    private string? _operationCompleteMessage;

    private object? _returnValue;
    private bool _hasReturnValue;

    static OperationLog()
    {
        _propertiesGetter = (ref log) => log._properties;

        _log = (logger, logLevel, eventId, ref readonly messageData, exception, properties) =>
            logger.Log(
                logLevel,
                eventId,
                new LogState<ReadOnlyNameValuePairList<TNameValuePairList>>(in messageData, new(properties)),
                exception,
                LogState<ReadOnlyNameValuePairList<TNameValuePairList>>.Formatter);

        if (typeof(TNameValuePairList).IsValueType)
        {
            try
            {
                MethodInfo? openLogMethod = typeof(ILogger).GetMethod(nameof(ILogger.Log));
                Type logStateType = typeof(LogState<>).MakeGenericType(typeof(TNameValuePairList));
                ConstructorInfo? logStateConstructor = logStateType.GetConstructors().FirstOrDefault(ctor => ctor.GetParameters().Length > 0);
                FieldInfo? formatterField = logStateType.GetField(nameof(LogState<>.Formatter), BindingFlags.NonPublic | BindingFlags.Static);

                if (openLogMethod != null && logStateConstructor != null && formatterField != null)
                {
                    MethodInfo logMethod = openLogMethod.MakeGenericMethod(logStateType);

                    ParameterExpression loggerParameter = Expression.Parameter(typeof(ILogger), "logger");
                    ParameterExpression logLevelParameter = Expression.Parameter(typeof(LogLevel), "logLevel");
                    ParameterExpression eventIdParameter = Expression.Parameter(typeof(EventId), "eventId");
                    ParameterExpression messageDataParameter = Expression.Parameter(typeof(MessageData).MakeByRefType(), "messageData");
                    ParameterExpression exceptionParameter = Expression.Parameter(typeof(Exception), "exception");
                    ParameterExpression propertiesParameter = Expression.Parameter(typeof(TNameValuePairList), "properties");

                    Expression body = Expression.Call(
                        loggerParameter,
                        logMethod,
                        logLevelParameter,
                        eventIdParameter,
                        Expression.New(logStateConstructor, messageDataParameter, propertiesParameter),
                        exceptionParameter,
                        Expression.Field(null, formatterField));

                    Expression<LogDelegate> logExpression =
                        Expression.Lambda<LogDelegate>(
                            body, loggerParameter, logLevelParameter, eventIdParameter, messageDataParameter, exceptionParameter, propertiesParameter);

                    _log = logExpression.Compile();
                }
            }
            catch
            {
            }
        }
        else if (typeof(TNameValuePairList) == typeof(List<KeyValuePair<string, object?>>))
        {
            _propertiesGetter = (ref log) =>
                log._properties ??= (TNameValuePairList)(object)new List<KeyValuePair<string, object?>>();
        }
    }

    internal OperationLog(
        ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        TNameValuePairList properties)
    {
        // Always set the properties and event id fields.
        _properties = properties;
        _eventId = eventId;

        if (logger != null && operationName != null)
        {
            MessageData message = new($"Operation starting: {operationName}");
            _log(logger, logLevel, eventId, in message, null, properties);

            // Only set the rest of the fields if we know logging is enabled.
            _logger = logger;
            _logLevel = logLevel;
            _operationCompleteMessage = $"Operation complete: {operationName}";
            _stringBuilder = StringBuilderPool.Instance.Get();
        }
    }

    /// <inheritdoc/>
    public readonly EventId EventId => _eventId;

    /// <summary>
    /// Gets the collection of properties associated with the operation log.
    /// </summary>
    public TNameValuePairList Properties => _propertiesGetter(ref this);

    IReadOnlyList<KeyValuePair<string, object?>> IOperationLog.Properties => Properties;

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
            NameValuePairList2 additionalNameValuePairs = new();
            if (_hasReturnValue)
                additionalNameValuePairs.Add(new("ReturnValue", _returnValue));
            if (_stringBuilder.Length > 0)
                additionalNameValuePairs.Add(new("OperationLog", _stringBuilder.ToString()));

            MessageData message = new(_operationCompleteMessage, in additionalNameValuePairs);
            _log(_logger!, _logLevel, _eventId, in message, _exception, _properties);

            StringBuilderPool.Instance.Return(_stringBuilder);
        }

        this = default;
    }
}
