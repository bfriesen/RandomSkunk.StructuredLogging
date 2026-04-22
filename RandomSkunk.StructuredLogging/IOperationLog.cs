using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging
{
    internal interface IOperationLogInternal : IOperationLog
    {
        public StringBuilder? StringBuilder { get; }
    }

    /// <summary>
    /// Defines the interface for an operation log. When disposed, writes a structured log indicating that the operation is
    /// complete.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// public class Calculator(ILogger&lt;Calculator> logger)
    /// {
    ///     public int Divide(int dividend, int divisor, int? fallbackValue = null)
    ///     {
    ///         using IOperationLog log = logger.LogOperation(LogLevel.Information, 1286859363, $"{typeof(Calculator)}.{nameof(Divide)}", dividend, divisor, fallbackValue);
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
    public interface IOperationLog : IDisposable
    {
        /// <summary>
        /// Gets the collection of parameters associated with the operation log.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, object?>> Parameters { get; }

        /// <summary>
        /// Gets the event id associated with the operation log.
        /// </summary>
        EventId EventId { get; }

        /// <summary>
        /// Appends the interpolated log entry string to the operation log.
        /// </summary>
        /// <param name="logEntry">The log entry to add to the operation log.</param>
        void Append(
            [InterpolatedStringHandlerArgument("")]
            ref InterpolatedString.OperationLogEntry logEntry);

        /// <summary>
        /// Sets the <c>ReturnValue</c> property of the operation complete log and returns the same value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="returnValue">The return value of the operation.</param>
        /// <param name="returnValueExpression">The string representation of the original expression passed as the return value. This
        /// is automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The same return value.</returns>
        T ReturnValue<T>(
            T returnValue,
            [CallerArgumentExpression(nameof(returnValue))] string returnValueExpression = null!);

        /// <summary>
        /// Sets the exception of the operation complete log and returns the same exception.
        /// </summary>
        /// <typeparam name="TException">The type of exception to set and return. Must derive from Exception.</typeparam>
        /// <param name="exception">The exception instance to set as the current error.</param>
        /// <param name="exceptionExpression">The string representation of the original expression passed as the exception. This is
        /// automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The same exception instance provided in the exception parameter.</returns>
        TException Exception<TException>(
            TException exception,
            [CallerArgumentExpression(nameof(exception))] string exceptionExpression = null!)
            where TException : Exception;

        /// <summary>
        /// Adds a log entry containing the specified value and its original expression to the completion log's <c>OperationLog</c>
        /// property, then returns the same value.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `httpResponse.StatusCode` is NotFound</c>" to
        /// the operation log:
        /// <code>
        /// if ((int)log.Value(httpResponse.StatusCode) is >= 200 and &lt;= 299)
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The value to log and return.</param>
        /// <param name="valueExpression">The string representation of the original expression passed as the value. This is
        /// automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The <paramref name="value"/> parameter.</returns>
        T Value<T>(
            T value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Adds a log entry containing the specified value, JSON serialized, and its original expression to the completion log's
        /// <c>OperationLog</c> property, then returns the same value.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `contact` is
        /// {"firstName":"Joe","lastName":"Public"}</c>" to the operation log:
        /// <code>
        /// log.JsonValue(contact);
        /// </code>
        /// </remarks>
        /// <param name="value">The value to log and return.</param>
        /// <param name="options">Options to control serialization behavior.</param>
        /// <param name="valueExpression">The string representation of the original expression passed as the value. This is
        /// automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The <paramref name="value"/> parameter.</returns>
        T JsonValue<T>(
            T value,
            JsonSerializerOptions? options = null,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Adds a log entry containing the specified boolean condition and its original expression to the completion log's
        /// <c>OperationLog</c> property, then returns the same value.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `divisor == 0` is true</c>" to the operation
        /// log:
        /// <code>
        /// if (log.Condition(divisor == 0))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="condition">The boolean condition to log and return.</param>
        /// <param name="conditionExpression">The string representation of the original expression passed as the condition. This is
        /// automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The <paramref name="condition"/> parameter.</returns>
        bool Condition(
            bool condition,
            [CallerArgumentExpression(nameof(condition))]
            string conditionExpression = null!);

        /// <summary>
        /// Returns whether the specified value is null. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is null.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `firstName == null` is true</c>" to the
        /// operation log:
        /// <code>
        /// if (log.IsNull(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null; otherwise, false.</returns>
        bool IsNull<T>(
            [NotNullWhen(false)] T value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!)
            where T : class?;

        /// <summary>
        /// Returns whether the specified value is null. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is null.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `monthlyIncome == null` is true</c>" to the
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
        bool IsNull<T>(
            [NotNullWhen(false)]
            T? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!)
            where T : struct;

        /// <summary>
        /// Returns whether the specified value is null or empty. If logging is enabled for the operation, also appends a log entry
        /// to the operation log that indicates whether the value is null or empty.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `string.IsNullOrEmpty(firstName)` is true</c>"
        /// to the operation log:
        /// <code>
        /// if (log.IsNullOrEmpty(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null or empty; otherwise, false.</returns>
        bool IsNullOrEmpty(
            [NotNullWhen(false)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Returns whether the specified value is null or whitespace. If logging is enabled for the operation, also appends a log entry
        /// to the operation log that indicates whether the value is null or whitespace.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `string.IsNullOrWhiteSpace(firstName)` is
        /// true</c>" to the operation log:
        /// <code>
        /// if (log.IsNullOrWhiteSpace(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null or whitespace; otherwise, false.</returns>
        bool IsNullOrWhiteSpace(
            [NotNullWhen(false)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Returns whether the specified value is not null. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is not null.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `firstName != null` is true</c>" to the
        /// operation log:
        /// <code>
        /// if (log.IsNotNull(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is not null; otherwise, false.</returns>
        bool IsNotNull<T>(
            [NotNullWhen(true)]
            T value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!)
            where T : class?;

        /// <summary>
        /// Returns whether the specified value is not null. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is not null.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `monthlyIncome != null` is true</c>" to the
        /// operation log:
        /// <code>
        /// if (log.IsNotNull(monthlyIncome))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is not null; otherwise, false.</returns>
        bool IsNotNull<T>(
            [NotNullWhen(true)]
            T? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!)
            where T : struct;

        /// <summary>
        /// Returns whether the specified value is not null or empty. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is not null or empty.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `!string.IsNullOrEmpty(firstName)` is true</c>"
        /// to the operation log:
        /// <code>
        /// if (log.IsNotNullOrEmpty(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is not null or empty; otherwise, false.</returns>
        bool IsNotNullOrEmpty(
            [NotNullWhen(true)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Returns whether the specified value is not null or whitespace. If logging is enabled for the operation, also appends a log entry to the
        /// operation log that indicates whether the value is not null or whitespace.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `!string.IsNullOrWhiteSpace(firstName)` is
        /// true</c>" to the operation log:
        /// <code>
        /// if (log.IsNotNullOrWhiteSpace(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The object to check.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is not null or whitespace; otherwise, false.</returns>
        bool IsNotNullOrWhiteSpace(
            [NotNullWhen(true)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);
    }
}