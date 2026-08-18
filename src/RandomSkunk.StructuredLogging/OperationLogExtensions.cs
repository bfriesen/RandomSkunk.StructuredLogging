using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension methods for recording a value returned from an operation (or sub-operation) as that
/// operation's result, or appending a value to its journal.
/// </summary>
public static class OperationLogExtensions
{
    /// <summary>
    /// Records <paramref name="result"/> as the result of the operation <paramref name="log"/> belongs to,
    /// then returns <paramref name="result"/> unchanged - so this can be chained directly onto a return
    /// expression, e.g. <c>return OrderResult.Shipped(...).OperationLogSetResult(log);</c>. See
    /// <see cref="IOperationLog.SetResult{T}"/>/<see cref="ISubOperationLog.SetResult{T}"/> for what
    /// recording a result actually does on the root operation vs. a sub-operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The operation (or sub-operation) the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T OperationLogSetResult<T>(this T result, IOperationLog log)
    {
        log.SetResult(result);
        return result;
    }

    /// <summary>
    /// Appends <paramref name="value"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can
    /// be chained directly onto an expression, e.g. <c>var total = order.Total.OperationLogAppendValue(log);</c>.
    /// See <see cref="IOperationLog.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="log">The operation (or sub-operation) to append the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T OperationLogAppendValue<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        log.AppendValue(value, valueName);
        return value;
    }
}
