using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags an interpolation hole (e.g. <c>{value:&lt;UserId}</c>) in a message argument passed to
/// one of the RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write
/// extension methods whose format specifier starts with '&lt;' but has no matching '&gt;' - almost
/// always a missing '&gt;' typo, since a real format that needs to start with a literal '&lt;'
/// should use the <c>&lt;&gt;</c> escape hatch instead. At run time this throws
/// <c>UnterminatedLogPropertyTagException</c>; this analyzer catches the same mistake at compile
/// time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnterminatedLogPropertyTagAnalyzer : DiagnosticAnalyzer
{
    // The ref struct interpolated string handler types declared in
    // LogInterpolatedStringHandlers.g.cs - one per level, plus WriteInterpolatedStringHandler
    // for Write. An interpolated string argument's format specifier is only ever parsed for the
    // <PropertyName> tag format (by AppendFormatted, at run time) when it's converted to one of
    // these types, so that conversion is exactly what scopes this analyzer to
    // RandomSkunk.StructuredLogging message arguments rather than any other interpolated string
    // in the compilation.
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
        ImmutableArray.Create(DiagnosticDescriptors.UnterminatedLogPropertyTag);

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
        if (formatClause is null)
            return;

        if (!LogPropertyTagFormatParsing.IsUnterminatedTag(formatClause.FormatStringToken.ValueText))
            return;

        if (interpolation.Parent is not InterpolatedStringExpressionSyntax interpolatedString)
            return;

        ITypeSymbol? convertedType = context.SemanticModel.GetTypeInfo(interpolatedString, context.CancellationToken).ConvertedType;
        if (convertedType is not INamedTypeSymbol namedType || !handlerTypes.Contains(namedType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.UnterminatedLogPropertyTag,
            interpolation.GetLocation()));
    }
}
