using System.Diagnostics.CodeAnalysis;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Extension method for recording a value returned from an operation (or sub-operation) as that
/// operation's result.
/// </summary>
public static class OperationResultExtensions
{
    /// <summary>
    /// Records <paramref name="result"/> as the result of the operation <paramref name="log"/> belongs to,
    /// then returns <paramref name="result"/> unchanged - so this can be chained directly onto a return
    /// expression, e.g. <c>return OrderResult.Shipped(...).SetOperationResult(log);</c>. See
    /// <see cref="IOperationLog.SetResult{T}"/>/<see cref="ISubOperationLog.SetResult{T}"/> for what
    /// recording a result actually does on the root operation vs. a sub-operation.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The operation (or sub-operation) the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T SetOperationResult<T>(this T result, IOperationLog log)
    {
        log.SetResult(result);
        return result;
    }
}
