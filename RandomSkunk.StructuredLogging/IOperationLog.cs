using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging
{
    internal interface IOperationLogInternal : IOperationLog
    {
        public StringBuilder? StringBuilder { get; }
    }

    /// <summary>
    /// Defines the interface for an operation log that writes its contents when disposed.
    /// </summary>
    public interface IOperationLog : IDisposable
    {
        /// <summary>
        /// Gets the event id of the operation log.
        /// </summary>
        EventId EventId { get; }

        /// <summary>
        /// Gets the list of name value pairs associated with the operation.
        /// </summary>
        List<KeyValuePair<string, object?>> Properties { get; }

        /// <summary>
        /// Appends the interpolated log entry string to the operation log.
        /// </summary>
        /// <param name="logEntry">The log entry to add to the operation log.</param>
        void Append(
            [InterpolatedStringHandlerArgument("")]
            ref InterpolatedString.OperationLogEntry logEntry);

        /// <summary>
        /// Sets the <c>Operation.ReturnValue</c> property of the operation log and returns the same value.
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
        /// Sets the exception of the operation log and returns the same exception.
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
        /// Adds a log entry containing the specified value and its original expression to the operation log's <c>Operation.Log</c>
        /// property, then returns the same value.
        /// </summary>
        /// <remarks>The following example adds an entry similar to "<c>[20:57:24.615Z] `httpResponse.StatusCode` is NotFound</c>" to
        /// the operation log:
        /// <code>
        /// if ((int)log.Value(httpResponse.StatusCode) is >= 200 and &lt;= 299)
        ///     ...
        /// </code>
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to log and return.</param>
        /// <param name="valueExpression">The string representation of the original expression passed as the value. This is
        /// automatically provided by the compiler and should not be set manually.</param>
        /// <returns>The <paramref name="value"/> parameter.</returns>
        T Value<T>(
            T value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Adds a log entry containing the specified boolean value and its original expression to the operation log's
        /// <c>Operation.Log</c> property, then returns the same value.
        /// </summary>
        /// <remarks>The following example adds an entry in the format "<c>[20:57:24.615Z] `divisor == 0` is [true|false]</c>" to
        /// the operation log:
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
        /// Returns whether the specified value is null and adds a log entry to the operation log's <c>Operation.Log</c> property
        /// indicating as such.
        /// </summary>
        /// <remarks>The following example adds an entry in the format "<c>[20:57:24.615Z] `firstName` is [not] null</c>" to the
        /// operation log:
        /// <code>
        /// if (log.IsNull(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The object to test.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null; otherwise, false.</returns>
        bool IsNull<T>(
            [NotNullWhen(false)] T value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!)
            where T : class?;

        /// <summary>
        /// Returns whether the specified value is null and adds a log entry to the operation log's <c>Operation.Log</c> property
        /// indicating as such.
        /// </summary>
        /// <remarks>The following example adds an entry in the format "<c>[20:57:24.615Z] `monthlyIncome` is [not] null</c>" to
        /// the operation log:
        /// <code>
        /// if (log.IsNull(monthlyIncome))
        ///     ...
        /// </code>
        /// </remarks>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The object to test.</param>
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
        /// Returns whether the specified value is null or empty and adds a log entry to the operation log's <c>Operation.Log</c>
        /// property indicating as such.
        /// </summary>
        /// <remarks>The following example adds an entry in the format "<c>[20:57:24.615Z] `firstName` is [not] null or
        /// empty</c>" to the operation log:
        /// <code>
        /// if (log.IsNullOrEmpty(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The string to test.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null or empty; otherwise, false.</returns>
        bool IsNullOrEmpty(
            [NotNullWhen(false)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);

        /// <summary>
        /// Returns whether the specified value is null or whitespace and adds a log entry to the operation log's
        /// <c>Operation.Log</c> property indicating as such.
        /// </summary>
        /// <remarks>The following example adds an entry in the format "<c>[20:57:24.615Z] `firstName` is [not] null or
        /// whitespace</c>" to the operation log:
        /// <code>
        /// if (log.IsNullOrWhiteSpace(firstName))
        ///     ...
        /// </code>
        /// </remarks>
        /// <param name="value">The string to test.</param>
        /// <param name="valueExpression">The expression passed for the value parameter. This is automatically provided by the
        /// compiler.</param>
        /// <returns>true if the specified object is null or whitespace; otherwise, false.</returns>
        bool IsNullOrWhiteSpace(
            [NotNullWhen(false)]
            string? value,
            [CallerArgumentExpression(nameof(value))]
            string valueExpression = null!);
    }
}