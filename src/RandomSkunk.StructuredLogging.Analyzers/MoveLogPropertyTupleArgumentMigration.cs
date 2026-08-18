using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement invocation for a call flagged by <see cref="LogPropertyTupleArgumentAnalyzer"/>
/// (RSSL0004) - see <see cref="LogPropertyTupleArgumentCodeFixProvider"/>. Removes the flagged
/// name/value tuple argument and moves its value into the message's interpolated string as a
/// <c>&lt;PropertyName&gt;</c> tag-format interpolation hole instead - the inverse of
/// <see cref="LogPropertyTagFormatMigration"/> (which extracts a tag-format hole out to a trailing
/// tuple argument).
///
/// <para>
/// If the message already contains an empty interpolation hole (<c>{}</c> - a hole with no
/// expression, which only ever appears mid-edit since it doesn't compile on its own) the tuple's
/// value fills that hole directly, e.g. <c>logger.Trace($"Hello, {}!", ("Who", who))</c> becomes
/// <c>logger.Trace($"Hello, {who:&lt;Who&gt;}!")</c>. Otherwise a new hole is appended to the end
/// of the message, preceded by a literal space, e.g. <c>logger.Trace($"Hello, there!", ("Who", who))</c>
/// becomes <c>logger.Trace($"Hello, there! {who:&lt;Who&gt;}")</c>. A plain (non-interpolated)
/// string literal message is promoted to an interpolated string first, following the same two rules
/// against its text.
/// </para>
/// </summary>
internal static class MoveLogPropertyTupleArgumentMigration
{
    /// <summary>
    /// Returns the original and rewritten invocation for the call containing <paramref name="tuple"/>,
    /// along with the property name it captures, or <see langword="null"/> if <paramref name="tuple"/>
    /// isn't a structured log property tuple directly passed as an argument to a call whose message
    /// argument this migration knows how to rewrite.
    /// </summary>
    public static (InvocationExpressionSyntax OldInvocation, InvocationExpressionSyntax NewInvocation, string PropertyName)? TryCreateReplacement(
        TupleExpressionSyntax tuple, SemanticModel semanticModel)
    {
        if (tuple.Arguments.Count != 2)
            return null;

        if (tuple.Parent is not ArgumentSyntax tupleArgument ||
            tupleArgument.Parent is not ArgumentListSyntax argumentList ||
            argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return null;

        int messageParameterIndex = -1;
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            if (method.Parameters[i].Name == "message")
            {
                messageParameterIndex = i;
                break;
            }
        }

        if (messageParameterIndex < 0 || messageParameterIndex >= argumentList.Arguments.Count)
            return null;

        ArgumentSyntax messageArgument = argumentList.Arguments[messageParameterIndex];

        string? propertyName = ConstantStringExpressionParsing.TryGetConstantStringValue(tuple.Arguments[0].Expression, semanticModel);
        if (propertyName is null)
            return null;

        ExpressionSyntax valueExpression = tuple.Arguments[1].Expression.WithoutTrivia();

        InterpolatedStringExpressionSyntax? newMessageExpression = messageArgument.Expression switch
        {
            InterpolatedStringExpressionSyntax interpolatedString => AddHoleToInterpolatedString(interpolatedString, valueExpression, propertyName),
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal => PromoteStringLiteral(literal, valueExpression, propertyName),
            _ => null,
        };

        if (newMessageExpression is null)
            return null;

        IEnumerable<ArgumentSyntax> newArguments = argumentList.Arguments
            .Where(argument => argument != tupleArgument)
            .Select(argument => argument == messageArgument ? argument.WithExpression(newMessageExpression) : argument);

        InvocationExpressionSyntax newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArguments)));

        return (invocation, newInvocation, propertyName);
    }

    private static InterpolatedStringExpressionSyntax AddHoleToInterpolatedString(
        InterpolatedStringExpressionSyntax interpolatedString, ExpressionSyntax valueExpression, string propertyName)
    {
        InterpolationSyntax? emptyHole = interpolatedString.Contents
            .OfType<InterpolationSyntax>()
            .FirstOrDefault(interpolation => interpolation.Expression.IsMissing);

        if (emptyHole is not null)
        {
            InterpolationSyntax filledHole = emptyHole.WithExpression(valueExpression).WithFormatClause(CreateTagFormatClause(propertyName));
            return interpolatedString.WithContents(SyntaxFactory.List(
                interpolatedString.Contents.Select(content => content == emptyHole ? filledHole : content)));
        }

        InterpolationSyntax newHole = SyntaxFactory.Interpolation(valueExpression).WithFormatClause(CreateTagFormatClause(propertyName));

        SyntaxList<InterpolatedStringContentSyntax> contents = interpolatedString.Contents;
        if (contents.Count > 0 && contents[contents.Count - 1] is InterpolatedStringTextSyntax lastText)
        {
            InterpolatedStringTextSyntax spacedText = CreateTextPiece(lastText.TextToken.ValueText + " ");
            contents = contents.Replace(lastText, spacedText);
        }
        else
        {
            contents = contents.Add(CreateTextPiece(" "));
        }

        return interpolatedString.WithContents(contents.Add(newHole));
    }

    private static InterpolatedStringExpressionSyntax PromoteStringLiteral(
        LiteralExpressionSyntax literal, ExpressionSyntax valueExpression, string propertyName)
    {
        string text = literal.Token.ValueText;
        InterpolationSyntax newHole = SyntaxFactory.Interpolation(valueExpression).WithFormatClause(CreateTagFormatClause(propertyName));

        List<InterpolatedStringContentSyntax> contents = new();

        int markerIndex = text.IndexOf("{}", StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            string before = text.Substring(0, markerIndex);
            string after = text.Substring(markerIndex + 2);

            if (before.Length > 0)
                contents.Add(CreateTextPiece(before));

            contents.Add(newHole);

            if (after.Length > 0)
                contents.Add(CreateTextPiece(after));
        }
        else
        {
            contents.Add(CreateTextPiece(text + " "));
            contents.Add(newHole);
        }

        return SyntaxFactory.InterpolatedStringExpression(
            SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
            SyntaxFactory.List(contents),
            SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
    }

    private static InterpolationFormatClauseSyntax CreateTagFormatClause(string propertyName)
    {
        string formatText = $"<{propertyName}>";
        SyntaxToken formatToken = SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, formatText, formatText, default);
        return SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), formatToken);
    }

    private static InterpolatedStringTextSyntax CreateTextPiece(string rawValue)
    {
        string escapedText = SymbolDisplay.FormatLiteral(rawValue, quote: false).Replace("{", "{{").Replace("}", "}}");
        SyntaxToken textToken = SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, escapedText, rawValue, default);
        return SyntaxFactory.InterpolatedStringText(textToken);
    }
}
