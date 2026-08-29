using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Shared by <see cref="UndisposedOperationLogAnalyzer"/> (RSSL0006) and its code fix provider.
/// <c>IOperationLogBase&lt;TOperationLog&gt;</c>'s <c>AddProperty</c>/<c>Append</c>/<c>AppendValue</c>/
/// <c>AppendJson</c>/<c>AppendException</c>/<c>AppendResult</c>/<c>Escalate</c> methods - inherited by
/// both <c>IOperationLog</c> and <c>ISubOperationLog</c> - and <c>IOperationLog</c>'s additional
/// <c>SetException</c>/<c>SetResult</c>, are all fluent - they return the same instance they were
/// called on - so a value produced by <c>BeginOperation</c>/<c>BeginSubOperation</c> is still that
/// same, still-undisposed operation log after being threaded through any chain of these calls, e.g.
/// <c>logger.BeginOperation("Op").AddProperty("Name", value)</c>. Both the analyzer and the code fix
/// need to reason about the *outermost* link in such a chain - the expression a developer would
/// actually assign, wrap in a <c>using</c>, or dispose - rather than the inner
/// <c>BeginOperation</c>/<c>BeginSubOperation</c> call the diagnostic anchors on.
/// </summary>
internal static class OperationLogChain
{
    private static readonly ImmutableHashSet<string> FluentMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "AddProperty",
        "Append",
        "AppendValue",
        "AppendJson",
        "AppendException",
        "AppendResult",
        "SetException",
        "SetResult");

    /// <summary>
    /// Walks from <paramref name="operationLogValue"/> (an expression whose value is an
    /// <c>IOperationLog</c>/<c>ISubOperationLog</c>) up through any chain of fluent calls made
    /// directly on it, returning the outermost link: the expression whose value is what a developer
    /// would actually need to dispose. Most of these methods (all but <c>SetException</c>/
    /// <c>SetResult</c>) are inherited from the generic <c>IOperationLogBase&lt;TOperationLog&gt;</c>, so
    /// Roslyn reports their containing type as a closed instantiation of it
    /// (<c>IOperationLogBase&lt;IOperationLog&gt;</c>/<c>IOperationLogBase&lt;ISubOperationLog&gt;</c>)
    /// rather than <c>IOperationLog</c>/<c>ISubOperationLog</c> themselves - matched here via
    /// <paramref name="operationLogBaseType"/> (the open generic definition) and
    /// <see cref="ISymbol.OriginalDefinition"/>. <c>SetException</c>/<c>SetResult</c> are declared
    /// directly on <c>IOperationLog</c>, so those match <paramref name="operationLogType"/> directly.
    /// </summary>
    public static IOperation GetOutermost(IOperation operationLogValue, INamedTypeSymbol operationLogType, INamedTypeSymbol operationLogBaseType)
    {
        IOperation current = SkipConversions(operationLogValue);

        while (current.Parent is IInvocationOperation outer
            && outer.Instance == current
            && FluentMethodNames.Contains(outer.TargetMethod.Name)
            && (SymbolEqualityComparer.Default.Equals(outer.TargetMethod.ContainingType, operationLogType)
                || SymbolEqualityComparer.Default.Equals(outer.TargetMethod.ContainingType.OriginalDefinition, operationLogBaseType)))
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
