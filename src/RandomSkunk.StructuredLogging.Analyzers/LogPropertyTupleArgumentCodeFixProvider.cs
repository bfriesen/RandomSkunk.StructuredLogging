using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Offers to move a name/value tuple argument flagged by <see cref="LogPropertyTupleArgumentAnalyzer"/>
/// (RSSL0004) into the message's interpolated string as a <c>&lt;PropertyName&gt;</c> tag-format
/// interpolation hole - see <see cref="MoveLogPropertyTupleArgumentMigration"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LogPropertyTupleArgumentCodeFixProvider))]
[Shared]
public sealed class LogPropertyTupleArgumentCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LogPropertyTupleArgument.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not TupleExpressionSyntax tuple)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var replacement = MoveLogPropertyTupleArgumentMigration.TryCreateReplacement(tuple, semanticModel);
        if (replacement is null)
            return;

        var (oldInvocation, newInvocation, propertyName) = replacement.Value;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Move '{propertyName}' into the message",
                createChangedDocument: _ => Task.FromResult(
                    context.Document.WithSyntaxRoot(root.ReplaceNode(oldInvocation, newInvocation))),
                equivalenceKey: nameof(LogPropertyTupleArgumentCodeFixProvider)),
            diagnostic);
    }
}
