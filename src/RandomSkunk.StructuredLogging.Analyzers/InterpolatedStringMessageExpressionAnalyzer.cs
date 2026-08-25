using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Flags a call to one of the RandomSkunk.StructuredLogging
/// Trace/Debug/Information/Warning/Error/Critical/Write extension methods whose <c>message</c>
/// argument applies a method call or <c>+</c> concatenation directly to an interpolated string
/// literal (e.g. <c>logger.Debug($"User {id}".ToUpper())</c> or
/// <c>logger.Debug($"count: " + count)</c>). Either pattern forces the call to bind the plain
/// <c>string message</c> overload instead of the interpolated-string-handler overload, silently
/// defeating both the disabled-level evaluation optimization and the
/// <c>&lt;PropertyName&gt;</c> tag format.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InterpolatedStringMessageExpressionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.InterpolatedStringOperationAppliedToMessageArgument);

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

            if (TryDescribeDirectOperation(argument.Value) is { } description)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InterpolatedStringOperationAppliedToMessageArgument,
                    argument.Value.Syntax.GetLocation(),
                    description));
            }

            return;
        }
    }

    /// <summary>
    /// If <paramref name="value"/> is a method call or <c>+</c> concatenation applied directly to
    /// an interpolated string literal - not through an intervening local variable, which is
    /// <c>RSSL0007</c>'s concern instead - returns a short description of the operation for the
    /// diagnostic message; otherwise returns <see langword="null"/>.
    /// </summary>
    private static string? TryDescribeDirectOperation(IOperation value)
    {
        IOperation unwrapped = Unwrap(value);

        switch (unwrapped)
        {
            case IInvocationOperation { Instance: { } instance } invocation:
                return ContainsInterpolatedStringLiteral(instance)
                    ? $"'.{invocation.TargetMethod.Name}(...)'"
                    : null;

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                return ContainsInterpolatedStringLiteral(binary.LeftOperand) || ContainsInterpolatedStringLiteral(binary.RightOperand)
                    ? "'+' concatenation"
                    : null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Returns whether <paramref name="operation"/>, or the receiver it was called/concatenated on
    /// (recursively, for a chained call or concatenation), is directly an interpolated string
    /// literal - i.e. without passing through a local variable, field, property, or any other
    /// indirection along the way.
    /// </summary>
    private static bool ContainsInterpolatedStringLiteral(IOperation operation)
    {
        IOperation unwrapped = Unwrap(operation);

        switch (unwrapped)
        {
            case IInterpolatedStringOperation:
                return true;

            case IInvocationOperation { Instance: { } instance }:
                return ContainsInterpolatedStringLiteral(instance);

            case IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } binary:
                return ContainsInterpolatedStringLiteral(binary.LeftOperand) || ContainsInterpolatedStringLiteral(binary.RightOperand);

            default:
                return false;
        }
    }

    private static IOperation Unwrap(IOperation operation)
    {
        IOperation unwrapped = operation;
        while (unwrapped is IConversionOperation conversion)
            unwrapped = conversion.Operand;

        return unwrapped;
    }
}
