using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Extension methods for recording a value to an operation's (or sub-operation's) journal, or recording a
/// value returned from an operation as that operation's result. All but <see cref="RecordResultTo{T}"/> are
/// generic over a <c>TLog</c> type parameter, constrained to <see cref="IOperationLogBase{TOperationLog}"/>, so a
/// single method works whether <c>log</c> is an <see cref="IOperationLog"/> or an
/// <see cref="ISubOperationLog"/> - the two are deliberately unrelated interfaces (see
/// <see cref="IOperationLogBase{TOperationLog}"/>), so without this a non-generic parameter typed as one would
/// reject the other.
/// </summary>
public static class OperationLogExtensions
{
    /// <summary>
    /// Sets <paramref name="result"/> as the <c>Operation.Result</c> of the root operation <paramref name="log"/>
    /// belongs to, then returns <paramref name="result"/> unchanged - so this can be chained directly onto a
    /// return expression, e.g. <c>return OrderResult.Shipped(...).RecordResultTo(log);</c>. See
    /// <see cref="IOperationLog.SetResult{T}"/>. Only available on the root <see cref="IOperationLog"/> -
    /// for a sub-operation, use <see cref="AppendResultTo{T, TLog}"/> instead.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The root operation the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T RecordResultTo<T>(this T result, IOperationLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.SetResult(result);
        return result;
    }

    /// <summary>
    /// Appends <paramref name="result"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`name` result: value</c>, then returns <paramref name="result"/> unchanged - so this
    /// can be chained directly onto a return expression, e.g.
    /// <c>return allocation.RecordAsShipped().AppendResultTo(subLog);</c>. See
    /// <see cref="IOperationLogBase{TOperationLog}.AppendResult{T}"/>. Available on the root operation or any
    /// sub-operation; on the root, use <see cref="RecordResultTo{T}"/> instead if the result should also
    /// become the final entry's <c>Operation.Result</c> structured property.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <typeparam name="TLog">The type of the operation (or sub-operation) log.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The operation (or sub-operation) the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T AppendResultTo<T, TLog>(this T result, TLog log)
        where TLog : IOperationLogBase<TLog>
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendResult(result);
        return result;
    }

    /// <summary>
    /// Adds <paramref name="propertyValue"/> as a structured property named <paramref name="propertyName"/>
    /// to the operation <paramref name="log"/> belongs to, then returns <paramref name="propertyValue"/>
    /// unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var orderId = order.Id.RecordPropertyTo(log, "OrderId");</c>. See
    /// <see cref="IOperationLogBase{TOperationLog}.AddProperty{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <typeparam name="TLog">The type of the operation (or sub-operation) log.</typeparam>
    /// <param name="propertyValue">The property value to record.</param>
    /// <param name="log">The operation (or sub-operation) to record the property to.</param>
    /// <param name="propertyName">The name to record the property under.</param>
    /// <returns><paramref name="propertyValue"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(propertyValue))]
    public static T RecordPropertyTo<T, TLog>(this T propertyValue, TLog log, string propertyName)
        where TLog : IOperationLogBase<TLog>
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AddProperty(propertyName, propertyValue);
        return propertyValue;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can
    /// be chained directly onto an expression, e.g. <c>var total = order.Total.RecordValueTo(log);</c>.
    /// See <see cref="IOperationLogBase{TOperationLog}.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLog">The type of the operation (or sub-operation) log.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The operation (or sub-operation) to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T RecordValueTo<T, TLog>(this T value, TLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
        where TLog : IOperationLogBase<TLog>
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendValue(value, valueName);
        return value;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`valueName`: value</c> with <paramref name="value"/> rendered as indented JSON, then
    /// returns <paramref name="value"/> unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var order = FetchOrder(id).RecordJsonTo(log);</c>. See
    /// <see cref="IOperationLogBase{TOperationLog}.AppendJson{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLog">The type of the operation (or sub-operation) log.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The operation (or sub-operation) to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T RecordJsonTo<T, TLog>(this T value, TLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
        where TLog : IOperationLogBase<TLog>
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendJson(value, valueName);
        return value;
    }
}
