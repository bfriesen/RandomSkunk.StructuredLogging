using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags calls to <c>Microsoft.Extensions.Logging.LoggerExtensions</c>'
/// <c>Log</c>/<c>LogTrace</c>/<c>LogDebug</c>/<c>LogInformation</c>/<c>LogWarning</c>/
/// <c>LogError</c>/<c>LogCritical</c> extension methods, which force structured properties
/// into the message template, and suggests using the corresponding
/// RandomSkunk.StructuredLogging extension method instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AvoidLoggerExtensionsAnalyzer : DiagnosticAnalyzer
{
    // The full set of extension method names declared by Microsoft.Extensions.Logging's
    // LoggerExtensions class. Checking both the containing type and this name set (rather than
    // just the containing type) keeps the analyzer scoped to exactly the "Log*" methods called
    // out by design, even if a future version of that class adds an unrelated extension method.
    private static readonly ImmutableHashSet<string> LoggerExtensionsMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Log",
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.AvoidLoggerExtensionsMethod);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            INamedTypeSymbol? loggerExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(
                "Microsoft.Extensions.Logging.LoggerExtensions");

            // Microsoft.Extensions.Logging.Abstractions isn't referenced by this compilation,
            // so there's nothing this analyzer could ever flag in it.
            if (loggerExtensionsType is null)
                return;

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, loggerExtensionsType),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol loggerExtensionsType)
    {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // A call like `logger.LogInformation(...)` resolves to the *reduced* form of the
        // extension method, whose ReceiverType is ILogger rather than LoggerExtensions.
        // ReducedFrom recovers the original static method so the containing-type check below
        // works the same way for both `logger.LogInformation(...)` and
        // `LoggerExtensions.LogInformation(logger, ...)` call syntax.
        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        if (!LoggerExtensionsMethodNames.Contains(method.Name))
            return;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, loggerExtensionsType))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AvoidLoggerExtensionsMethod,
            invocation.Syntax.GetLocation(),
            method.Name));
    }
}
