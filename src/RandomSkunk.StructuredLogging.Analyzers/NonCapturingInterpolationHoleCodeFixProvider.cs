using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Offers to add a <c>&lt;PropertyName&gt;</c> tag to an interpolation hole flagged by
/// <see cref="NonCapturingInterpolationHoleAnalyzer"/> (RSSL0003), so its value starts being
/// captured as a structured property - see <see cref="AddLogPropertyTagFormatMigration"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonCapturingInterpolationHoleCodeFixProvider))]
[Shared]
public sealed class NonCapturingInterpolationHoleCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NonCapturingInterpolationHole.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var diagnostic = context.Diagnostics[0];
        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is not InterpolationSyntax hole)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var replacement = AddLogPropertyTagFormatMigration.TryCreateReplacement(hole, semanticModel);
        if (replacement is null)
            return;

        var (oldHole, newHole, propertyName) = replacement.Value;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Capture as a structured property named '{propertyName}'",
                createChangedDocument: _ => Task.FromResult(
                    context.Document.WithSyntaxRoot(root.ReplaceNode(oldHole, newHole))),
                equivalenceKey: nameof(NonCapturingInterpolationHoleCodeFixProvider)),
            diagnostic);
    }
}
