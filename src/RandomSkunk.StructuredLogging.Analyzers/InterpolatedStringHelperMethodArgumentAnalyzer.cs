using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a call to one of the RandomSkunk.StructuredLogging
/// Trace/Debug/Information/Warning/Error/Critical/Write extension methods whose <c>message</c>
/// argument is the result of calling some other ("wrapper" or "helper") method that was itself
/// handed an interpolated string literal as one of its arguments (e.g.
/// <c>logger.Debug(FormatMessage($"User {id}"))</c>). Because the helper method's parameter isn't
/// one of this library's <c>[InterpolatedStringHandler]</c> types, the literal is built eagerly by
/// the ordinary compiler-provided handler regardless of whether the helper's caller checked
/// <c>IsEnabled</c> first - and whatever plain <c>string</c> the helper returns then binds
/// <c>logger.Debug</c>'s plain <c>string message</c> overload, silently defeating both the
/// disabled-level evaluation optimization and the <c>&lt;PropertyName&gt;</c> tag format.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterpolatedStringHelperMethodArgumentAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InterpolatedStringPassedToHelperMethodArgument);

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

            if (TryDescribeHelperMethodCall(argument.Value) is { } description)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InterpolatedStringPassedToHelperMethodArgument,
                    argument.Value.Syntax.GetLocation(),
                    description));
            }

            return;
        }
    }

    /// <summary>
    /// If <paramref name="value"/> is a call to some helper method that was itself handed an
    /// interpolated string literal as one of its arguments (directly, or through a chain of
    /// further helper-method calls) - not through an intervening local variable, which is
    /// <c>RSSL0007</c>'s concern instead - returns a short description of the helper method for
    /// the diagnostic message; otherwise returns <see langword="null"/>.
    /// </summary>
    private static string? TryDescribeHelperMethodCall(IOperation value)
    {
        if (Unwrap(value) is not IInvocationOperation invocation)
            return null;

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (ContainsInterpolatedStringLiteral(argument.Value))
                return $"'{invocation.TargetMethod.Name}(...)'";
        }

        return null;
    }

    /// <summary>
    /// Returns whether <paramref name="operation"/> is directly an interpolated string literal, or
    /// a call to a helper method that was itself (recursively) handed one as an argument - i.e.
    /// without passing through a local variable, field, property, or any other indirection along
    /// the way.
    /// </summary>
    private static bool ContainsInterpolatedStringLiteral(IOperation operation)
    {
        IOperation unwrapped = Unwrap(operation);

        if (unwrapped is IInterpolatedStringOperation)
            return true;

        if (unwrapped is IInvocationOperation invocation)
        {
            foreach (IArgumentOperation argument in invocation.Arguments)
            {
                if (ContainsInterpolatedStringLiteral(argument.Value))
                    return true;
            }
        }

        return false;
    }

    private static IOperation Unwrap(IOperation operation)
    {
        IOperation unwrapped = operation;
        while (unwrapped is IConversionOperation conversion)
            unwrapped = conversion.Operand;

        return unwrapped;
    }
}
