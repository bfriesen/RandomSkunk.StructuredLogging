using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement interpolation hole for a call flagged by <see cref="LogPropertyTagFormatAnalyzer"/>
/// (RSSL0002) - see <see cref="LogPropertyTagFormatCodeFixProvider"/>. Strips the
/// <c>&lt;PropertyName&gt;</c> tag prefix from the hole's format specifier, leaving the value
/// formatted in the message text as before but no longer captured as a structured property, e.g.
/// <c>{ts:&lt;Timestamp&gt;O}</c> becomes <c>{ts:O}</c>, and <c>{ts:&lt;Timestamp&gt;}</c> (nothing
/// after the tag) becomes <c>{ts}</c>. For a destructuring tag (<c>&lt;@PropertyName&gt;</c>), the
/// '@' is preserved as the empty <c>&lt;@&gt;</c> tag instead of stripping the tag entirely, so the
/// message keeps rendering with Serilog-style destructured formatting exactly as before - only the
/// capture is removed, e.g. <c>{item:&lt;@Item&gt;}</c> becomes <c>{item:&lt;@&gt;}</c>.
/// </summary>
internal static class RemoveLogPropertyTagFormatMigration
{
    /// <summary>
    /// Returns the original and rewritten interpolation hole for <paramref name="hole"/>, along
    /// with the property name it was capturing, or <see langword="null"/> if <paramref name="hole"/>
    /// isn't a tag-format hole.
    /// </summary>
    public static (InterpolationSyntax OldHole, InterpolationSyntax NewHole, string PropertyName)? TryCreateReplacement(InterpolationSyntax hole)
    {
        var formatClause = hole.FormatClause;
        if (formatClause is null)
            return null;

        var format = formatClause.FormatStringToken.ValueText;
        var propertyName = LogPropertyTagFormatParsing.TryGetPropertyName(format);
        if (propertyName is null)
            return null;

        var remainingFormat = LogPropertyTagFormatParsing.GetRemainingFormat(format);
        var destructure = LogPropertyTagFormatParsing.IsDestructuring(format);

        InterpolationSyntax newHole;
        if (destructure)
        {
            var newFormatText = remainingFormat is null ? "<@>" : $"<@>{remainingFormat}";
            newHole = hole.WithFormatClause(formatClause.WithFormatStringToken(
                SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, newFormatText, newFormatText, default)));
        }
        else
        {
            newHole = remainingFormat is null
                ? hole.WithFormatClause(null)
                : hole.WithFormatClause(formatClause.WithFormatStringToken(
                    SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, remainingFormat, remainingFormat, default)));
        }

        return (hole, newHole, propertyName);
    }
}
