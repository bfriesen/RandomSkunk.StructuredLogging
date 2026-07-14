using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Offers two rewrites for an interpolation hole flagged by <see cref="LogPropertyTagFormatAnalyzer"/>
/// (RSSL0002): extracting it from the message's interpolated string into a trailing
/// <c>(string Name, object? Value)</c> tuple argument (see <see cref="LogPropertyTagFormatMigration"/>),
/// or simply stripping the <c>&lt;PropertyName&gt;</c> tag so the value stays in the message but is
/// no longer captured as a structured property (see <see cref="RemoveLogPropertyTagFormatMigration"/>).
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

        var extractReplacement = LogPropertyTagFormatMigration.TryCreateReplacement(hole);
        if (extractReplacement is not null)
        {
            var (oldInvocation, newInvocation, propertyName) = extractReplacement.Value;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Move '{propertyName}' to a structured property argument",
                    createChangedDocument: _ => Task.FromResult(
                        context.Document.WithSyntaxRoot(root.ReplaceNode(oldInvocation, newInvocation))),
                    equivalenceKey: $"{nameof(LogPropertyTagFormatCodeFixProvider)}.Extract"),
                diagnostic);
        }

        var removeReplacement = RemoveLogPropertyTagFormatMigration.TryCreateReplacement(hole);
        if (removeReplacement is not null)
        {
            var (oldHole, newHole, propertyName) = removeReplacement.Value;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Remove the '{propertyName}' tag format, keeping it in the message only",
                    createChangedDocument: _ => Task.FromResult(
                        context.Document.WithSyntaxRoot(root.ReplaceNode(oldHole, newHole))),
                    equivalenceKey: $"{nameof(LogPropertyTagFormatCodeFixProvider)}.Remove"),
                diagnostic);
        }
    }
}
