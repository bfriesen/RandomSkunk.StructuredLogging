using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Rewrites an interpolation hole flagged by <see cref="LogPropertyTagFormatAnalyzer"/> (RSSL0002)
/// by removing it from the message's interpolated string and appending its value as a trailing
/// <c>(string Name, object? Value)</c> tuple argument instead - see <see cref="LogPropertyTagFormatMigration"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LogPropertyTagFormatCodeFixProvider))]
[Shared]
public sealed class LogPropertyTagFormatCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LogPropertyTagFormatInterpolationHole.Id);

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

        var replacement = LogPropertyTagFormatMigration.TryCreateReplacement(hole);
        if (replacement is null)
            return;

        var (oldInvocation, newInvocation, propertyName) = replacement.Value;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{propertyName}' to a structured property argument",
                createChangedDocument: _ => Task.FromResult(
                    context.Document.WithSyntaxRoot(root.ReplaceNode(oldInvocation, newInvocation))),
                equivalenceKey: nameof(LogPropertyTagFormatCodeFixProvider)),
            diagnostic);
    }
}
