using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Determines whether an expression's value is knowable at compile time as a <see cref="string"/>,
/// for a broader notion of "constant" than the C# language's own (<see cref="SemanticModel.GetConstantValue(SyntaxNode, System.Threading.CancellationToken)"/>)
/// covers - the interpolated-string case below is not a <em>constant expression</em> per the C#
/// spec (interpolated strings only fold to a constant when they contain no interpolations at all),
/// even though its value is still fully knowable at compile time when every hole's own value is.
/// Used by <see cref="LogPropertyTupleArgumentAnalyzer"/> (RSSL0004) to decide whether a tuple
/// argument's name is safe to treat as a fixed structured property name.
/// </summary>
internal static class ConstantStringExpressionParsing
{
    /// <summary>
    /// Returns the compile-time value of <paramref name="expression"/> if it's provably a constant
    /// <see cref="string"/>: a string literal, a reference to a <see langword="const"/> string, a
    /// <c>+</c> concatenation of constant strings, a parenthesized constant string, an interpolated
    /// string with no holes, or an interpolated string whose every hole's expression is itself a
    /// constant string (recursively) - or <see langword="null"/> if it isn't.
    /// </summary>
    public static string? TryGetConstantStringValue(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var constantValue = semanticModel.GetConstantValue(expression);
        if (constantValue.HasValue)
            return constantValue.Value as string;

        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return TryGetConstantStringValue(parenthesized.Expression, semanticModel);

            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression } concatenation:
                var left = TryGetConstantStringValue(concatenation.Left, semanticModel);
                if (left is null)
                    return null;

                var right = TryGetConstantStringValue(concatenation.Right, semanticModel);
                return right is null ? null : left + right;

            case InterpolatedStringExpressionSyntax interpolatedString:
                return TryGetConstantInterpolatedStringValue(interpolatedString, semanticModel);

            default:
                return null;
        }
    }

    private static string? TryGetConstantInterpolatedStringValue(InterpolatedStringExpressionSyntax interpolatedString, SemanticModel semanticModel)
    {
        var value = new StringBuilder();

        foreach (var content in interpolatedString.Contents)
        {
            switch (content)
            {
                case InterpolatedStringTextSyntax text:
                    value.Append(text.TextToken.ValueText);
                    break;

                case InterpolationSyntax interpolation:
                    var holeValue = TryGetConstantStringValue(interpolation.Expression, semanticModel);
                    if (holeValue is null)
                        return null;

                    value.Append(holeValue);
                    break;
            }
        }

        return value.ToString();
    }
}
