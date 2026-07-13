using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags an interpolation hole (e.g. <c>{who:&lt;Recipient&gt;}</c>) in a message argument passed
/// to one of the RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write
/// extension methods that uses the <c>&lt;PropertyName&gt;</c> tag format to capture the
/// interpolated value as a structured property. Reported at "silent" (<see cref="DiagnosticSeverity.Hidden"/>)
/// severity - this analyzer exists purely as an anchor for code fixes (registered separately) that
/// act on these holes, not to warn about anything wrong with the code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LogPropertyTagFormatAnalyzer : DiagnosticAnalyzer
{
    // The ref struct interpolated string handler types declared in
    // Generated/LogInterpolatedStringHandlers.g.cs - one per level, plus LogInterpolatedStringHandler
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
        "RandomSkunk.StructuredLogging.LogInterpolatedStringHandler");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.LogPropertyTagFormatInterpolationHole);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var handlerTypesBuilder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var metadataName in HandlerTypeMetadataNames)
            {
                var type = compilationContext.Compilation.GetTypeByMetadataName(metadataName);
                if (type is not null)
                    handlerTypesBuilder.Add(type);
            }

            // RandomSkunk.StructuredLogging isn't referenced by this compilation, so there's
            // nothing this analyzer could ever flag in it.
            if (handlerTypesBuilder.Count == 0)
                return;

            var handlerTypes = handlerTypesBuilder.ToImmutable();

            compilationContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeInterpolation(syntaxContext, handlerTypes),
                SyntaxKind.Interpolation);
        });
    }

    private static void AnalyzeInterpolation(SyntaxNodeAnalysisContext context, ImmutableHashSet<INamedTypeSymbol> handlerTypes)
    {
        var interpolation = (InterpolationSyntax)context.Node;

        var formatClause = interpolation.FormatClause;
        if (formatClause is null)
            return;

        var propertyName = LogPropertyTagFormatParsing.TryGetPropertyName(formatClause.FormatStringToken.ValueText);
        if (propertyName is null)
            return;

        if (interpolation.Parent is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        var convertedType = context.SemanticModel.GetTypeInfo(interpolatedString, context.CancellationToken).ConvertedType;
        if (convertedType is not INamedTypeSymbol namedType || !handlerTypes.Contains(namedType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.LogPropertyTagFormatInterpolationHole,
            interpolation.GetLocation(),
            propertyName));
    }
}
