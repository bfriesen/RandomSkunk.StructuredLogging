using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Rewrites a call flagged by <see cref="AvoidLoggerExtensionsAnalyzer"/> (RSSL0001) into the
/// equivalent RandomSkunk.StructuredLogging extension method call. Each <c>{PropertyName}</c>
/// message-template placeholder becomes a <c>{value:&lt;PropertyName&gt;}</c> interpolation hole,
/// so the same value is still shown in the message text and still captured as a structured
/// property under the same name.
/// </summary>
/// <remarks>
/// Only offered when the call's message argument is a string literal and its placeholder count
/// matches the number of trailing format arguments; anything else (a non-literal message, a
/// mismatched placeholder count, or an explicit <c>object[]</c> passed instead of individual
/// arguments) is left for the developer to migrate by hand rather than risk an incorrect rewrite.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AvoidLoggerExtensionsCodeFixProvider))]
[Shared]
public sealed class AvoidLoggerExtensionsCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.AvoidLoggerExtensionsMethod.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        Diagnostic diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InvocationExpressionSyntax invocation)
            return;

        SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation)
            return;

        ExpressionSyntax? replacement = LoggerExtensionsMigration.TryCreateReplacement(operation);
        if (replacement is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use the equivalent RandomSkunk.StructuredLogging extension method",
                createChangedDocument: _ => Task.FromResult(
                    context.Document.WithSyntaxRoot(root.ReplaceNode(invocation, replacement.WithTriviaFrom(invocation)))),
                equivalenceKey: nameof(AvoidLoggerExtensionsCodeFixProvider)),
            diagnostic);
    }
}
