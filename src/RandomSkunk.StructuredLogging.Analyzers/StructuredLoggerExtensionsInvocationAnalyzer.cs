using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a call to any of the RandomSkunk.StructuredLogging
/// Trace/Debug/Information/Warning/Error/Critical/Write extension methods - unlike
/// <see cref="LogPropertyTagFormatAnalyzer"/> (RSSL0002), <see cref="NonCapturingInterpolationHoleAnalyzer"/>
/// (RSSL0003), and <see cref="LogPropertyTupleArgumentAnalyzer"/> (RSSL0004), which each flag a
/// specific part of such a call, this analyzer flags the call itself, regardless of which overload
/// is used. Reported at "silent" (<see cref="DiagnosticSeverity.Hidden"/>) severity - this analyzer
/// exists purely as an anchor for code fixes (registered separately) that act on these calls, not
/// to warn about anything wrong with the code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructuredLoggerExtensionsInvocationAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.StructuredLoggerExtensionsInvocation);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            INamedTypeSymbol? structuredLoggerExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(
                "RandomSkunk.StructuredLogging.StructuredLoggerExtensions");

            // RandomSkunk.StructuredLogging isn't referenced by this compilation, so there's
            // nothing this analyzer could ever flag in it.
            if (structuredLoggerExtensionsType is null)
                return;

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, structuredLoggerExtensionsType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol structuredLoggerExtensionsType)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // A call like `logger.Debug(...)` resolves to the *reduced* form of the extension method,
        // whose ReceiverType is ILogger rather than StructuredLoggerExtensions. ReducedFrom
        // recovers the original static method so the containing-type check below works the same
        // way for both `logger.Debug(...)` and `StructuredLoggerExtensions.Debug(logger, ...)`
        // call syntax.
        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, structuredLoggerExtensionsType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StructuredLoggerExtensionsInvocation,
            invocation.Syntax.GetLocation(),
            method.Name));
    }
}
