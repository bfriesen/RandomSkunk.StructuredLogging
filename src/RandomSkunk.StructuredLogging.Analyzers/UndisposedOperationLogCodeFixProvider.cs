using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Offers two fixes for a call flagged by <see cref="UndisposedOperationLogAnalyzer"/> (RSSL0006):
/// turning the flagged statement into a <c>using</c> declaration (see
/// <see cref="AddUsingDeclarationMigration"/>), or wrapping it in an empty <c>using</c> block for
/// the developer to fill in (see <see cref="AddUsingBlockMigration"/>). Both are offered only when
/// the invocation is the entire expression of an expression statement (including a discarded
/// <c>_ = ...;</c> assignment) or the initializer of a single-variable local declaration that
/// doesn't already use <c>using</c> - the invocation nested inside a larger expression (e.g.
/// passed as an argument) is left for the developer to fix by hand.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UndisposedOperationLogCodeFixProvider))]
[Shared]
public sealed class UndisposedOperationLogCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UndisposedOperationLog.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        Diagnostic diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax invocationSyntax)
            return;

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocationSyntax, context.CancellationToken) is not IInvocationOperation invocation)
            return;

        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        INamedTypeSymbol? operationLogType = semanticModel.Compilation.GetTypeByMetadataName(
            "RandomSkunk.StructuredLogging.Operation.IOperationLog");
        INamedTypeSymbol? operationLogBaseType = semanticModel.Compilation.GetTypeByMetadataName(
            "RandomSkunk.StructuredLogging.Operation.IOperationLogBase`1");
        if (operationLogType is null || operationLogBaseType is null)
            return;

        // IOperationLog's/ISubOperationLog's fluent AddProperty/Append/AppendValue/AppendJson/
        // AppendException/AppendResult/SetException/SetResult methods all return the same instance,
        // so a call like `logger.BeginOperation("Op").AddProperty("Name", value)` is still building
        // the exact value that needs disposing - the outermost link in the chain, not the inner
        // BeginOperation/BeginSubOperation call the diagnostic anchors on, is what the fixes below
        // need to treat as "the operation-log-producing expression".
        SyntaxNode operationLogExpression = OperationLogChain.GetOutermost(invocation, operationLogType, operationLogBaseType).Syntax;

        StatementSyntax? anchorStatement = operationLogExpression.FirstAncestorOrSelf<StatementSyntax>();
        if (anchorStatement is null)
            return;

        (StatementSyntax OldStatement, StatementSyntax NewStatement)? declarationReplacement =
            AddUsingDeclarationMigration.TryCreateReplacement(semanticModel, anchorStatement, operationLogExpression, method);
        if (declarationReplacement is not null)
        {
            (StatementSyntax oldStatement, StatementSyntax newStatement) = declarationReplacement.Value;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add a 'using' declaration",
                    createChangedDocument: _ => Task.FromResult(
                        context.Document.WithSyntaxRoot(root.ReplaceNode(oldStatement, newStatement))),
                    equivalenceKey: $"{nameof(UndisposedOperationLogCodeFixProvider)}.Declaration"),
                diagnostic);
        }

        (StatementSyntax OldStatement, StatementSyntax NewStatement)? blockReplacement =
            AddUsingBlockMigration.TryCreateReplacement(semanticModel, anchorStatement, operationLogExpression, method);
        if (blockReplacement is not null)
        {
            (StatementSyntax oldStatement, StatementSyntax newStatement) = blockReplacement.Value;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add a 'using' block",
                    createChangedDocument: _ => Task.FromResult(
                        context.Document.WithSyntaxRoot(root.ReplaceNode(oldStatement, newStatement))),
                    equivalenceKey: $"{nameof(UndisposedOperationLogCodeFixProvider)}.Block"),
                diagnostic);
        }
    }
}
