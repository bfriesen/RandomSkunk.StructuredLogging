using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags an interpolation hole (e.g. <c>{who}</c>) in a message argument passed to one of the
/// RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write extension
/// methods that does <em>not</em> use the <c>&lt;PropertyName&gt;</c> tag format to capture the
/// interpolated value as a structured property - the mirror image of
/// <see cref="LogPropertyTagFormatAnalyzer"/> (RSSL0002). Reported at "silent"
/// (<see cref="DiagnosticSeverity.Hidden"/>) severity - this analyzer exists purely as an anchor
/// for code fixes (registered separately) that act on these holes, not to warn about anything
/// wrong with the code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonCapturingInterpolationHoleAnalyzer : DiagnosticAnalyzer
{
    // The ref struct interpolated string handler types declared in
    // LogInterpolatedStringHandlers.g.cs - one per level, plus WriteInterpolatedStringHandler
    // for Write. An interpolated string argument is only ever parsed for the <PropertyName> tag
    // format (by AppendFormatted, at run time) when it's converted to one of these types, so that
    // conversion is exactly what scopes this analyzer to RandomSkunk.StructuredLogging message
    // arguments rather than any other interpolated string in the compilation.
    private static readonly ImmutableArray<string> HandlerTypeMetadataNames = ImmutableArray.Create(
        "RandomSkunk.StructuredLogging.TraceInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.DebugInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.InformationInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.WarningInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.ErrorInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.CriticalInterpolatedStringHandler",
        "RandomSkunk.StructuredLogging.WriteInterpolatedStringHandler");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.NonCapturingInterpolationHole);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            ImmutableHashSet<INamedTypeSymbol>.Builder handlerTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (string metadataName in HandlerTypeMetadataNames)
            {
                INamedTypeSymbol? type = compilationContext.Compilation.GetTypeByMetadataName(metadataName);
                if (type is not null)
                    handlerTypesBuilder.Add(type);
            }

            // RandomSkunk.StructuredLogging isn't referenced by this compilation, so there's
            // nothing this analyzer could ever flag in it.
            if (handlerTypesBuilder.Count == 0)
                return;

            ImmutableHashSet<INamedTypeSymbol> handlerTypes = handlerTypesBuilder.ToImmutable();

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInterpolation(syntaxContext, handlerTypes),
                SyntaxKind.Interpolation);
        });
    }

    private static void AnalyzeInterpolation(SyntaxNodeAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> handlerTypes)
    {
        InterpolationSyntax interpolation = (InterpolationSyntax)context.Node;

        InterpolationFormatClauseSyntax? formatClause = interpolation.FormatClause;
        if (formatClause is not null)
        {
            string? propertyName = LogPropertyTagFormatParsing.TryGetPropertyName(formatClause.FormatStringToken.ValueText);
            if (propertyName is not null)
                return;
        }

        if (interpolation.Parent is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        ITypeSymbol? convertedType = context.SemanticModel.GetTypeInfo(interpolatedString, context.CancellationToken).ConvertedType;
        if (convertedType is not INamedTypeSymbol namedType || !handlerTypes.Contains(namedType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NonCapturingInterpolationHole,
            interpolation.GetLocation(),
            interpolation.Expression.ToString()));
    }
}
