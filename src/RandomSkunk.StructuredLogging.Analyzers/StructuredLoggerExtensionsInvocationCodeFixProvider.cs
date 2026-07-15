using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Rewrites a call flagged by <see cref="StructuredLoggerExtensionsInvocationAnalyzer"/> (RSSL0005)
/// into the "roughly equivalent" <c>Microsoft.Extensions.Logging.LoggerExtensions</c> call - see
/// <see cref="StructuredLoggerExtensionsInvocationMigration"/> for exactly what can and can't be
/// carried over.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StructuredLoggerExtensionsInvocationCodeFixProvider))]
[Shared]
public sealed class StructuredLoggerExtensionsInvocationCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.StructuredLoggerExtensionsInvocation.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax invocation)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null || semanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation)
            return;

        var replacement = StructuredLoggerExtensionsInvocationMigration.TryCreateReplacement(operation, semanticModel);
        if (replacement is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use the equivalent Microsoft.Extensions.Logging extension method",
                createChangedDocument: _ => Task.FromResult(
                    context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replacement.WithTriviaFrom(invocation)))),
                equivalenceKey: nameof(StructuredLoggerExtensionsInvocationCodeFixProvider)),
            diagnostic);
    }
}
