using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a call to <c>BeginOperation</c>/<c>BeginSubOperation</c> whose returned
/// <c>IOperationLog</c> isn't disposed. This is the operation-logging analog of CA2000, but keyed
/// off of these specific factory methods rather than a <c>new</c>-expression: since a consumer
/// only ever obtains an <c>IOperationLog</c> from one of them (never <c>new</c>), CA2000's
/// escape analysis - which anchors on <c>new</c>-expressions visible in the consuming
/// compilation - can't see through them (<c>RootOperationLog</c>/<c>DisabledOperationLog</c> are
/// constructed inside the already-compiled library assembly).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndisposedOperationLogAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UndisposedOperationLog);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            INamedTypeSymbol? loggerOperationExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(
                "RandomSkunk.StructuredLogging.Operation.LoggerOperationExtensions");
            INamedTypeSymbol? operationLogType = compilationContext.Compilation.GetTypeByMetadataName(
                "RandomSkunk.StructuredLogging.Operation.IOperationLog");
            INamedTypeSymbol? subOperationLogType = compilationContext.Compilation.GetTypeByMetadataName(
                "RandomSkunk.StructuredLogging.Operation.ISubOperationLog");

            // RandomSkunk.StructuredLogging isn't referenced by this compilation, so there's
            // nothing this analyzer could ever flag in it.
            if (loggerOperationExtensionsType is null || operationLogType is null || subOperationLogType is null)
                return;

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, loggerOperationExtensionsType, operationLogType, subOperationLogType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context, INamedTypeSymbol loggerOperationExtensionsType, INamedTypeSymbol operationLogType, INamedTypeSymbol subOperationLogType)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // `logger.BeginOperation(...)`/`subLog.BeginSubOperation(...)` resolve to the *reduced*
        // form of the (extension or interface) method, whose ReceiverType is ILogger/ISubOperationLog
        // rather than LoggerOperationExtensions. ReducedFrom recovers the original method so the
        // containing-type check below works the same way regardless of call syntax.
        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        // BeginSubOperation is declared separately on both IOperationLog and ISubOperationLog (the two
        // interfaces are unrelated), so a call through either receiver type resolves to that
        // interface's own copy - matched here against both directly.
        bool isBeginOperation = method.Name == "BeginOperation"
            && SymbolEqualityComparer.Default.Equals(method.ContainingType, loggerOperationExtensionsType);
        bool isBeginSubOperation = method.Name == "BeginSubOperation"
            && (SymbolEqualityComparer.Default.Equals(method.ContainingType, operationLogType)
                || SymbolEqualityComparer.Default.Equals(method.ContainingType, subOperationLogType));

        if (!isBeginOperation && !isBeginSubOperation)
            return;

        if (IsDisposedOrEscapes(invocation, operationLogType, subOperationLogType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UndisposedOperationLog,
            invocation.Syntax.GetLocation(),
            method.Name));
    }

    /// <summary>
    /// Whether the <c>IOperationLog</c>/<c>ISubOperationLog</c> returned by <paramref name="invocation"/>
    /// is visibly disposed, or escapes this method in a way that makes disposal someone else's
    /// responsibility (returned, or assigned to a field/property). Passing it as a plain argument
    /// does *not* count - developers are expected to create and dispose sub-operations within the
    /// method they're threaded into, not hand off ownership of the root/an existing sub-operation
    /// through a parameter. Considers the value's fate after any chain of fluent
    /// <c>AddProperty</c>/<c>Append</c>/<c>AppendValue</c>/<c>AppendJson</c>/<c>AppendException</c>/
    /// <c>AppendResult</c>/<c>SetException</c>/<c>SetResult</c> calls made directly on it, e.g.
    /// <c>using var log = logger.BeginOperation("Op").AddProperty("Name", value);</c> counts as
    /// disposed even though <paramref name="invocation"/> (the <c>BeginOperation</c> call) isn't
    /// itself the declaration's initializer - see <see cref="OperationLogChain"/>.
    /// </summary>
    private static bool IsDisposedOrEscapes(IInvocationOperation invocation, INamedTypeSymbol operationLogType, INamedTypeSymbol subOperationLogType)
    {
        IOperation effective = OperationLogChain.GetOutermost(invocation, operationLogType, subOperationLogType);

        // A declarator's initializer value is wrapped in an IVariableInitializerOperation, e.g.
        // `IOperationLog log = logger.BeginOperation(...);` is
        // IVariableDeclarator(log) -> IVariableInitializer -> IInvocationOperation.
        IOperation? parent = effective.Parent;
        if (parent is IVariableInitializerOperation initializer)
            parent = initializer.Parent;

        // `using var log = logger.BeginOperation(...);` / `using (var log = logger.BeginOperation(...)) { }`
        if (parent is IVariableDeclaratorOperation declarator)
            return IsLocalDisposedOrEscapes(declarator, operationLogType, subOperationLogType);

        return IndicatesDisposalOrEscape(parent);
    }

    private static bool IsLocalDisposedOrEscapes(IVariableDeclaratorOperation declarator, INamedTypeSymbol operationLogType, INamedTypeSymbol subOperationLogType)
    {
        // `using var log = logger.BeginOperation(...);` (a using declaration) or
        // `using (var log = logger.BeginOperation(...)) { }` (a using statement) - checked via
        // syntax rather than operation kind, since a using *declaration* isn't wrapped in its own
        // IUsingOperation the way a using *statement* is; both forms are reachable from the
        // declarator's syntax the same way.
        LocalDeclarationStatementSyntax? localDeclaration = declarator.Syntax.FirstAncestorOrSelf<LocalDeclarationStatementSyntax>();
        if (localDeclaration is not null && !localDeclaration.UsingKeyword.IsKind(SyntaxKind.None))
            return true;

        if (declarator.Syntax.FirstAncestorOrSelf<UsingStatementSyntax>() is not null)
            return true;

        ILocalSymbol local = declarator.Symbol;

        // Not declared with `using` - look for a later reference to the same local that disposes
        // it, uses it as a plain (non-declaring) `using` resource, or lets it escape - including
        // through a chain of fluent calls made directly on it, e.g. `log.AddProperty(...).Dispose();`.
        foreach (ILocalReferenceOperation localReference in FindLocalReferences(RootOf(declarator), local))
        {
            IOperation effective = OperationLogChain.GetOutermost(localReference, operationLogType, subOperationLogType);
            if (IndicatesDisposalOrEscape(effective.Parent))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Whether <paramref name="parent"/> - the parent of an operation-log-producing expression,
    /// after unwrapping any conversions - indicates the operation log is disposed or escapes.
    /// </summary>
    private static bool IndicatesDisposalOrEscape(IOperation? parent) => parent switch
    {
        // `log.Dispose();` (including via an explicit interface cast, e.g. `((IDisposable)log).Dispose();`)
        IInvocationOperation { TargetMethod.Name: "Dispose", Arguments.Length: 0 } => true,

        // `using (log) { }` - an existing variable used directly as a using resource.
        IUsingOperation => true,

        // `return log;` / `return logger.BeginOperation(...);`
        IReturnOperation => true,

        // Assigned to a field/property - assume the containing type disposes it elsewhere.
        ISimpleAssignmentOperation { Target: IFieldReferenceOperation or IPropertyReferenceOperation } => true,

        _ => false,
    };

    private static IOperation RootOf(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is not null)
            current = current.Parent;
        return current;
    }

    private static IEnumerable<ILocalReferenceOperation> FindLocalReferences(IOperation root, ILocalSymbol local)
    {
        if (root is ILocalReferenceOperation localReference && SymbolEqualityComparer.Default.Equals(localReference.Local, local))
            yield return localReference;

        foreach (IOperation child in root.ChildOperations)
        {
            foreach (ILocalReferenceOperation descendant in FindLocalReferences(child, local))
                yield return descendant;
        }
    }
}
