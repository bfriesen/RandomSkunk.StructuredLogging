using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

/// <summary>
/// Compiles a C# source snippet against Microsoft.Extensions.Logging.Abstractions and
/// RandomSkunk.StructuredLogging and runs a given analyzer against it, so tests can assert on the
/// resulting diagnostics without pulling in a separate analyzer-testing framework.
/// </summary>
internal static class AnalyzerVerifier
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, DiagnosticAnalyzer analyzer)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            [syntaxTree],
            TestReferences.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Diagnostic[] compilerErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilerErrors.Length > 0)
            throw new InvalidOperationException($"Test source failed to compile: {string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString()))}");

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers([analyzer]);

        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}
