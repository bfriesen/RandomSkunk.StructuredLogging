using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension method for appending a value to an operation's journal.
/// </summary>
public static class OperationValueExtensions
{
    /// <summary>
    /// Appends <paramref name="value"/> to the journal of the operation <paramref name="log"/> belongs to,
    /// in the form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can
    /// be chained directly onto an expression, e.g. <c>var total = order.Total.AppendOperationValue(log);</c>.
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
    public static T AppendOperationValue<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        log.AppendValue(value, valueName);
        return value;
    }
}
