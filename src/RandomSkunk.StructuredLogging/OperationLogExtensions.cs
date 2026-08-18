using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension methods for recording a value to an operation's (or sub-operation's) journal, or recording a
/// value returned from an operation as that operation's result.
/// </summary>
public static class OperationLogExtensions
{
    /// <summary>
    /// Records <paramref name="result"/> as the result of the operation <paramref name="log"/> belongs to,
    /// then returns <paramref name="result"/> unchanged - so this can be chained directly onto a return
    /// expression, e.g. <c>return OrderResult.Shipped(...).RecordResultTo(log);</c>. See
    /// <see cref="IOperationLog.SetResult{T}"/>/<see cref="ISubOperationLog.SetResult{T}"/> for what
    /// recording a result actually does on the root operation vs. a sub-operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The operation (or sub-operation) the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T RecordResultTo<T>(this T result, IOperationLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.SetResult(result);
        return result;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can
    /// be chained directly onto an expression, e.g. <c>var total = order.Total.RecordValueTo(log);</c>.
    /// See <see cref="IOperationLog.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The operation (or sub-operation) to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T RecordValueTo<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
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
    /// <see cref="IOperationLog.AppendJson{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The operation (or sub-operation) to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T RecordJsonTo<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendJson(value, valueName);
        return value;
    }
}
