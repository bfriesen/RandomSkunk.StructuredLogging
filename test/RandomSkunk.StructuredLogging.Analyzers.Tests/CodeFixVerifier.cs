using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

/// <summary>
/// Runs a given analyzer and then a given code fix provider against a C# source snippet inside a
/// minimal <see cref="AdhocWorkspace"/>, so tests can assert on the fixed source (or on there
/// being no fix offered) without pulling in a separate analyzer-testing framework.
/// </summary>
internal static class CodeFixVerifier
{
    /// <summary>
    /// Applies the code fix to the single diagnostic expected from <paramref name="source"/> and
    /// returns the resulting source text, or <see langword="null"/> if no fix was offered. When
    /// the provider registers more than one code action for the diagnostic, <paramref name="selectAction"/>
    /// picks which one to apply; it's required in that case and otherwise ignored.
    /// </summary>
    /// <param name="allowCompilerErrors">
    /// By default, <paramref name="source"/> is required to compile cleanly, same as
    /// <see cref="AnalyzerVerifier"/>, to catch accidentally-broken test sources. Pass
    /// <see langword="true"/> for the rare fix that targets code a developer would only have
    /// written mid-edit - e.g. an empty <c>{}</c> interpolation hole, which doesn't compile on its
    /// own (CS1733) but is still a well-formed syntax tree a code fix can act on.
    /// </param>
    public static async Task<string?> TryApplyFixAsync(
        string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider,
        Func<CodeAction, bool>? selectAction = null, bool allowCompilerErrors = false)
    {
        using AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();
        DocumentId documentId = DocumentId.CreateNewId(projectId);

        Solution solution = workspace.CurrentSolution
            .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Latest))
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, TestReferences.All)
            .AddDocument(documentId, "Test.cs", source);

        Document document = solution.GetDocument(documentId)!;

        Compilation compilation = (await document.Project.GetCompilationAsync())!;
        if (!allowCompilerErrors)
        {
            Diagnostic[] compilerErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (compilerErrors.Length > 0)
                throw new InvalidOperationException($"Test source failed to compile: {string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString()))}");
        }

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers([analyzer]);
        ImmutableArray<Diagnostic> diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        Diagnostic diagnostic = diagnostics.Single();

        List<CodeAction> actions = new();
        CodeFixContext context = new(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context);

        if (actions.Count == 0)
            return null;

        CodeAction selectedAction = selectAction is null ? actions.Single() : actions.Single(selectAction);

        ImmutableArray<CodeActionOperation> operations = await selectedAction.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation applyChanges = operations.OfType<ApplyChangesOperation>().Single();
        Document newDocument = applyChanges.ChangedSolution.GetDocument(documentId)!;
        SyntaxNode? newRoot = await newDocument.GetSyntaxRootAsync();

        return newRoot!.ToFullString();
    }
}
