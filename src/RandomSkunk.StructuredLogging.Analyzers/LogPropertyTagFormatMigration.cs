using System.Collections.Generic;
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
/// <c>logger.Debug($"User logged in:", ("UserName", userName))</c> - trailing whitespace left behind
/// in the text piece immediately preceding the removed hole is trimmed (dropping that text piece
/// entirely if it becomes empty), so <c>logger.Debug($"Hello {who:&lt;Who&gt;} how are you?")</c>
/// becomes <c>logger.Debug($"Hello how are you?", ("Who", who))</c> rather than leaving a doubled
/// space behind. For a destructuring tag (<c>&lt;@PropertyName&gt;</c>), the value moved into the
/// tuple argument is always the raw value (same as a non-destructuring tag) - the destructured
/// message rendering is discarded along with the rest of the hole, consistent with how this fix
/// already discards any formatting (destructured or not) when moving a value out of the message.
/// The '@' prefix itself is preserved in the tuple's property name (e.g. <c>("@PropertyName", value)</c>),
/// so this fix is the exact inverse of <see cref="MoveLogPropertyTupleArgumentMigration"/>, which
/// treats a leading '@' in a tuple argument's name as literal text to carry back into the tag.
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
        InterpolationFormatClauseSyntax? formatClause = hole.FormatClause;
        if (formatClause is null)
            return null;

        string format = formatClause.FormatStringToken.ValueText;
        string? propertyName = LogPropertyTagFormatParsing.TryGetPropertyName(format);
        if (propertyName is null)
            return null;

        if (LogPropertyTagFormatParsing.IsDestructuring(format))
            propertyName = "@" + propertyName;

        if (hole.Parent is not InterpolatedStringExpressionSyntax interpolatedString ||
            interpolatedString.Parent is not ArgumentSyntax messageArgument ||
            messageArgument.Parent is not ArgumentListSyntax argumentList ||
            argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        InterpolatedStringExpressionSyntax newInterpolatedString = RemoveHole(interpolatedString, hole);

        LiteralExpressionSyntax propertyNameLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(propertyName));
        ExpressionSyntax valueExpression = hole.Expression.WithoutTrivia();

        ArgumentSyntax tupleArgument = SyntaxFactory.Argument(
            SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(new[]
            {
                SyntaxFactory.Argument(propertyNameLiteral),
                SyntaxFactory.Argument(valueExpression),
            })));

        IEnumerable<ArgumentSyntax> newArguments = argumentList.Arguments
            .Select(argument => argument == messageArgument ? argument.WithExpression(newInterpolatedString) : argument)
            .Append(tupleArgument);

        InvocationExpressionSyntax newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArguments)));

        return (invocation, newInvocation, propertyName);
    }

    private static InterpolatedStringExpressionSyntax RemoveHole(
        InterpolatedStringExpressionSyntax interpolatedString, InterpolationSyntax hole)
    {
        SyntaxList<InterpolatedStringContentSyntax> contents = interpolatedString.Contents;
        int holeIndex = contents.IndexOf(hole);

        List<InterpolatedStringContentSyntax> newContents = new();
        for (int i = 0; i < contents.Count; i++)
        {
            if (i == holeIndex)
                continue;

            InterpolatedStringContentSyntax content = contents[i];
            if (i == holeIndex - 1 && content is InterpolatedStringTextSyntax precedingText)
            {
                string trimmedValue = precedingText.TextToken.ValueText.TrimEnd();
                if (trimmedValue.Length == 0)
                    continue;

                content = precedingText.WithTextToken(CreateTextToken(trimmedValue));
            }

            newContents.Add(content);
        }

        return interpolatedString.WithContents(SyntaxFactory.List(newContents));
    }

    private static SyntaxToken CreateTextToken(string rawValue)
    {
        string escapedText = SymbolDisplay.FormatLiteral(rawValue, quote: false).Replace("{", "{{").Replace("}", "}}");
        return SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, escapedText, rawValue, default);
    }
}
