using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a name/value tuple argument (e.g. <c>("UserId", userId)</c>) passed at the end of a call
/// to one of the RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write
/// extension methods to attach a structured property - the tuple-argument counterpart of
/// <see cref="LogPropertyTagFormatAnalyzer"/> (RSSL0002) and <see cref="NonCapturingInterpolationHoleAnalyzer"/>
/// (RSSL0003), which flag interpolation holes rather than tuple arguments. Only matches when the
/// tuple's name is a compile-time constant string per <see cref="ConstantStringExpressionParsing"/>
/// (a literal, a constant concatenation, an interpolated string with no holes, or one whose holes
/// are themselves constant strings, etc.) - a runtime-computed name can't be moved into the message
/// or reported as a stable property name. Reported at "silent" (<see cref="DiagnosticSeverity.Hidden"/>)
/// severity - this analyzer exists purely as an anchor for code fixes (registered separately) that
/// act on these arguments, not to warn about anything wrong with the code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LogPropertyTupleArgumentAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LogPropertyTupleArgument);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var structuredLoggerExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(
                "RandomSkunk.StructuredLogging.StructuredLoggerExtensions");

            // RandomSkunk.StructuredLogging isn't referenced by this compilation, so there's
            // nothing this analyzer could ever flag in it.
            if (structuredLoggerExtensionsType is null)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeTupleExpression(syntaxContext, structuredLoggerExtensionsType),
                SyntaxKind.TupleExpression);
        });
    }

    private static void AnalyzeTupleExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol structuredLoggerExtensionsType)
    {
        var tuple = (TupleExpressionSyntax)context.Node;

        // A structured log property tuple is always a two-element (string Name, T Value) pair,
        // and is always passed directly as an argument - not nested inside some other expression
        // (e.g. an explicit array creation) - so both the call and the property's name/value are
        // recoverable straight from the tuple's syntax.
        if (tuple.Arguments.Count != 2)
            return;

        if (tuple.Parent is not ArgumentSyntax argument ||
            argument.Parent is not ArgumentListSyntax argumentList ||
            argumentList.Parent is not InvocationExpressionSyntax invocation)
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
            return;

        var method = symbol.ReducedFrom ?? symbol;
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, structuredLoggerExtensionsType))
            return;

        // Only a tuple whose name is provably fixed at compile time is safe to treat as a
        // structured property name - a runtime-computed name (e.g. a variable, a method call)
        // can't be captured by a code fix or reported as a stable name.
        var propertyName = ConstantStringExpressionParsing.TryGetConstantStringValue(tuple.Arguments[0].Expression, context.SemanticModel);
        if (propertyName is null)
            return;

        var valueText = tuple.Arguments[1].Expression.ToString();

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LogPropertyTupleArgument,
            tuple.GetLocation(),
            valueText,
            propertyName));
    }
}
