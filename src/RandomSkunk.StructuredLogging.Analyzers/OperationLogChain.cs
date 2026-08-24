using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Shared by <see cref="UndisposedOperationLogAnalyzer"/> (RSSL0006) and its code fix provider.
/// <c>IOperationLog</c>'s <c>AddProperty</c>/<c>Append</c>/<c>AppendValue</c>/<c>AppendJson</c>/
/// <c>SetException</c>/<c>SetResult</c> methods are all fluent - they return the same instance
/// they were called on - so a value produced by <c>BeginOperation</c>/<c>BeginSubOperation</c> is
/// still that same, still-undisposed <c>IOperationLog</c> after being threaded through any chain
/// of these calls, e.g. <c>logger.BeginOperation("Op").AddProperty("Name", value)</c>. Both the
/// analyzer and the code fix need to reason about the *outermost* link in such a chain - the
/// expression a developer would actually assign, wrap in a <c>using</c>, or dispose - rather than
/// the inner <c>BeginOperation</c>/<c>BeginSubOperation</c> call the diagnostic anchors on.
/// </summary>
internal static class OperationLogChain
{
    private static readonly ImmutableHashSet<string> FluentMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "AddProperty",
        "Append",
        "AppendValue",
        "AppendJson",
        "SetException",
        "SetResult");

    /// <summary>
    /// Walks from <paramref name="operationLogValue"/> (an expression whose value is an
    /// <c>IOperationLog</c>) up through any chain of fluent <c>IOperationLog</c> calls made
    /// directly on it, returning the outermost link - the expression whose value is what a
    /// developer would actually need to dispose.
    /// </summary>
    public static IOperation GetOutermost(IOperation operationLogValue, INamedTypeSymbol operationLogType)
    {
        IOperation current = SkipConversions(operationLogValue);

        while (current.Parent is IInvocationOperation outer
            && outer.Instance == current
            && FluentMethodNames.Contains(outer.TargetMethod.Name)
            && SymbolEqualityComparer.Default.Equals(outer.TargetMethod.ContainingType, operationLogType))
        {
            current = SkipConversions(outer);
        }

        return current;
    }

    private static IOperation SkipConversions(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is IConversionOperation conversion)
            current = conversion;
        return current;
    }
}
