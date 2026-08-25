using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a call to one of the RandomSkunk.StructuredLogging
/// Trace/Debug/Information/Warning/Error/Critical/Write extension methods whose <c>message</c>
/// argument is a local variable declared with an interpolated string initializer that captures at
/// least one <c>&lt;PropertyName&gt;</c> tag (e.g.
/// <c>string msg = $"User {id:&lt;UserId&gt;}"; logger.Debug(msg);</c>). Assigning the
/// interpolated string to a <c>string</c> local first forces the call to bind the plain
/// <c>string message</c> overload instead of the interpolated-string-handler overload, silently
/// defeating both the disabled-level evaluation optimization and the tag format (which becomes a
/// genuine, usually-invalid .NET format string handed to <c>IFormattable.ToString(format)</c>).
/// Only fires when a tag is actually at stake - a tagless interpolated string local only loses the
/// disabled-level optimization, which this analyzer doesn't police.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterpolatedStringLocalMessageAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InterpolatedStringAssignedToLocalMessageArgument);

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

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter is not { Name: "message" } parameter)
                continue;

            // The interpolated-string-handler overloads also name their parameter "message", but
            // its type is one of the ref struct handler types, not string - only the plain
            // string-typed overload is the one this analyzer cares about.
            if (parameter.Type.SpecialType != SpecialType.System_String)
                return;

            if (TryGetInterpolatedStringInitializer(argument.Value) is { } localName)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InterpolatedStringAssignedToLocalMessageArgument,
                    argument.Value.Syntax.GetLocation(),
                    localName));
            }

            return;
        }
    }

    /// <summary>
    /// If <paramref name="value"/> is a reference to a local variable declared with an
    /// interpolated string initializer that captures at least one <c>&lt;PropertyName&gt;</c> tag
    /// (e.g. <c>string msg = $"User {id:&lt;UserId&gt;}";</c>), returns the local's name; otherwise
    /// returns <see langword="null"/>.
    /// </summary>
    private static string? TryGetInterpolatedStringInitializer(IOperation value)
    {
        IOperation unwrapped = value;
        while (unwrapped is IConversionOperation conversion)
            unwrapped = conversion.Operand;

        if (unwrapped is not ILocalReferenceOperation { Local: ILocalSymbol local })
            return null;

        foreach (SyntaxReference syntaxReference in local.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: InterpolatedStringExpressionSyntax interpolatedString } &&
                InterpolatedStringTagFormatDetection.ContainsPropertyTag(interpolatedString))
            {
                return local.Name;
            }
        }

        return null;
    }
}
