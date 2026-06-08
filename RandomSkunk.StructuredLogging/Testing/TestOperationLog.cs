using Microsoft.Extensions.Logging;
using System.Text;

namespace RandomSkunk.StructuredLogging.Testing;

/// <summary>
/// Defines an implementation of the <see cref="IOperationLog"/> interface suitable for use as a mock or stub.
/// </summary>
public class TestOperationLog : IOperationLogInternal
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

    /// <summary>
    /// Invoked when the test operation log's <see cref="IDisposable.Dispose"/> method is called.
    /// </summary>
    public virtual void OnDispose()
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Append"/> method is called.
    /// </summary>
    /// <param name="logEntry">The log entry to add to the operation log.</param>
    public virtual void OnAppend(string logEntry)
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.ReturnValue"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="returnValue">The return value of the operation.</param>
    public virtual void OnReturnValue<T>(T returnValue)
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Exception"/> method is called.
    /// </summary>
    /// <typeparam name="TException">The type of exception to set. Must derive from Exception.</typeparam>
    /// <param name="exception">The exception instance to set as the current error.</param>
    public virtual void OnException<TException>(TException exception)
        where TException : Exception
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Value"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to log.</param>
    public virtual void OnValue<T>(T value)
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.Condition"/> method is called.
    /// </summary>
    /// <param name="condition">The boolean condition to log.</param>
    public virtual void OnCondition(bool condition)
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNull{T}(T, string)"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The object to test.</param>
    public virtual void OnIsNull<T>(T value)
        where T : class?
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNull{T}(T?, string)"/> method is called.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The object to test.</param>
    public virtual void OnIsNull<T>(T? value)
        where T : struct
    {
    }

    /// <summary>
    /// Invoked when the test operation log's <see cref="IOperationLog.IsNullOrEmpty"/> method is called.
    /// </summary>
    /// <param name="value">The string to test.</param>
    public virtual void OnIsNullOrEmpty(string? value)
    {
    }

    /// <summary>
    /// Invoked when test operation log's the <see cref="IOperationLog.IsNullOrWhiteSpace"/> method is called.
    /// </summary>
    /// <param name="value">The string to test.</param>
    public virtual void OnIsNullOrWhiteSpace(string? value)
    {
    }

    void IDisposable.Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            OnDispose();
            GC.SuppressFinalize(this);
        }
    }

    void IOperationLog.Append(ref InterpolatedString.OperationLogEntry logEntry)
    {
        if (!_isDisposed)
        {
            OnAppend(_stringBuilder?.ToString() ?? "");
        }
        _stringBuilder?.Clear();
    }

    T IOperationLog.ReturnValue<T>(T returnValue, string returnValueExpression)
    {
        if (!_isDisposed)
        {
            OnReturnValue(returnValue);
        }
        return returnValue;
    }

    TException IOperationLog.Exception<TException>(TException exception, string exceptionExpression)
    {
        if (!_isDisposed)
        {
            OnException(exception);
        }
        return exception;
    }

    T IOperationLog.Value<T>(T value, string valueExpression)
    {
        if (!_isDisposed)
        {
            OnValue(value);
        }
        return value;
    }

    bool IOperationLog.Condition(bool condition, string conditionExpression)
    {
        if (!_isDisposed)
        {
            OnCondition(condition);
        }
        return condition;
    }

    bool IOperationLog.IsNull<T>(T value, string valueExpression)
    {
        if (!_isDisposed)
        {
            OnIsNull(value);
        }
        return value is null;
    }

    bool IOperationLog.IsNull<T>(T? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            OnIsNull(value);
        }
        return !value.HasValue;
    }

    bool IOperationLog.IsNullOrEmpty(string? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            OnIsNullOrEmpty(value);
        }
        return string.IsNullOrEmpty(value);
    }

    bool IOperationLog.IsNullOrWhiteSpace(string? value, string valueExpression)
    {
        if (!_isDisposed)
        {
            OnIsNullOrWhiteSpace(value);
        }
        return string.IsNullOrWhiteSpace(value);
    }
}