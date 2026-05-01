using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines an implementation of the <see cref="IOperationLog"/> suitable for testing.
/// </summary>
public abstract class TestOperationLog : IOperationLogInternal
{
    private StringBuilder? _stringBuilder;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets the event id associated with the test operation log.
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// Gets or sets the collection of properties associated with the test operation log.
    /// </summary>
    public List<KeyValuePair<string, object?>> Properties { get => field ??= []; }

    StringBuilder? IOperationLogInternal.StringBuilder => _stringBuilder ??= new();

    /// Invoked when the test operation log's <see cref="IDisposable.Dispose"/> method is called.
    public abstract void Dispose();

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Append"/> method is called.
    /// </summary>
    /// <param name="logEntry">The log entry to add to the operation log.</param>
    public abstract void Append(string logEntry);

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.ReturnValue"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="returnValue">The return value of the operation.</param>
    public abstract void ReturnValue<T>(T returnValue);

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Exception"/> method is called.
    /// </summary>
    /// <typeparam name="TException">The type of exception to set. Must derive from Exception.</typeparam>
    /// <param name="exception">The exception instance to set as the current error.</param>
    public abstract void Exception<TException>(TException exception)
        where TException : Exception;

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Value"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to log.</param>
    public abstract void Value<T>(T value);

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Condition"/> method is called.
    /// </summary>
    /// <param name="condition">The boolean condition to log.</param>
    public abstract void Condition(bool condition);

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNull{T}(T, string)"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The object to test.</param>
    public abstract void IsNull<T>(T value)
        where T : class?;

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNull{T}(T?, string)"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The object to test.</param>
    public abstract void IsNull<T>(T? value)
        where T : struct;

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNullOrEmpty"/> method is called.
    /// </summary>
    /// <param name="value">The string to test.</param>
    public abstract void IsNullOrEmpty(string? value);

    /// <summary>
    /// Invoked when test operation log's the <see cref="IOperationLog.IsNullOrWhiteSpace"/> method is called.
    /// </summary>
    /// <param name="value">The string to test.</param>
    public abstract void IsNullOrWhiteSpace(string? value);

    void IDisposable.Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            Dispose();
            GC.SuppressFinalize(this);
        }
    }

    void IOperationLog.Append(ref InterpolatedString.OperationLogEntry logEntry)
    {
        if (!_isDisposed)
        {
            Append(_stringBuilder?.ToString() ?? "");
        }
        _stringBuilder?.Clear();
    }

    T IOperationLog.ReturnValue<T>(T returnValue, string returnValueExpression)
    {
        if (!_isDisposed)
        {
            ReturnValue(returnValue);
        }
        return returnValue;
    }

    TException IOperationLog.Exception<TException>(TException exception, string exceptionExpression)
    {
        if (!_isDisposed)
        {
            Exception(exception);
        }
        return exception;
    }

    T IOperationLog.Value<T>(T value, string valueExpression)
    {
        if (!_isDisposed)
        {
            Value(value);
        }
        return value;
    }

    bool IOperationLog.Condition(bool condition, string conditionExpression)
    {
        if (!_isDisposed)
        {
            Condition(condition);
        }
        return condition;
    }

    bool IOperationLog.IsNull<T>(T value, string valueExpression)
    {
        if (!_isDisposed)
        {
            IsNull(value);
        }
        return value is null;
    }

    bool IOperationLog.IsNull<T>(T? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            IsNull(value);
        }
        return !value.HasValue;
    }

    bool IOperationLog.IsNullOrEmpty(string? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            IsNullOrEmpty(value);
        }
        return string.IsNullOrEmpty(value);
    }

    bool IOperationLog.IsNullOrWhiteSpace(string? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            IsNullOrWhiteSpace(value);
        }
        return string.IsNullOrWhiteSpace(value);
    }
}