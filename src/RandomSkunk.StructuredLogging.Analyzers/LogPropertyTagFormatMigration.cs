using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement expression for a call flagged by <see cref="LogPropertyTagFormatAnalyzer"/>
/// (RSSL0002) - see <see cref="LogPropertyTagFormatCodeFixProvider"/>. Removes the flagged
/// interpolation hole from the message's interpolated string and appends its value as a trailing
/// <c>(string Name, object? Value)</c> tuple argument instead, e.g.
/// <c>logger.Debug($"User logged in: {userName:&lt;UserName&gt;}")</c> becomes
/// <c>logger.Debug($"User logged in: ", ("UserName", userName))</c> - the hole is removed as-is,
/// with no attempt to clean up whitespace it leaves behind.
/// </summary>
internal static class LogPropertyTagFormatMigration
{
    /// <summary>
    /// Returns the original and rewritten invocation for the call containing
    /// <paramref name="hole"/>, along with the property name it captures, or
    /// <see langword="null"/> if <paramref name="hole"/> isn't a tag-format hole directly inside a
    /// message argument's interpolated string.
    /// </summary>
    public static (InvocationExpressionSyntax OldInvocation, InvocationExpressionSyntax NewInvocation, string PropertyName)? TryCreateReplacement(InterpolationSyntax hole)
    {
        var formatClause = hole.FormatClause;
        if (formatClause is null)
            return null;

        var propertyName = LogPropertyTagFormatParsing.TryGetPropertyName(formatClause.FormatStringToken.ValueText);
        if (propertyName is null)
            return null;

        if (hole.Parent is not InterpolatedStringExpressionSyntax interpolatedString ||
            interpolatedString.Parent is not ArgumentSyntax messageArgument ||
            messageArgument.Parent is not ArgumentListSyntax argumentList ||
            argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        var newInterpolatedString = RemoveHole(interpolatedString, hole);

        var propertyNameLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(propertyName));
        var valueExpression = hole.Expression.WithoutTrivia();

        var tupleArgument = SyntaxFactory.Argument(
            SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Argument(propertyNameLiteral),
                SyntaxFactory.Argument(valueExpression),
            })));

        var newArguments = argumentList.Arguments
            .Select(argument => argument == messageArgument ? argument.WithExpression(newInterpolatedString) : argument)
            .Append(tupleArgument);

        var newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArguments)));

        return (invocation, newInvocation, propertyName);
    }

    private static InterpolatedStringExpressionSyntax RemoveHole(
        InterpolatedStringExpressionSyntax interpolatedString, InterpolationSyntax hole) =>
        interpolatedString.WithContents(SyntaxFactory.List(interpolatedString.Contents.Where(c => c != hole)));
}
