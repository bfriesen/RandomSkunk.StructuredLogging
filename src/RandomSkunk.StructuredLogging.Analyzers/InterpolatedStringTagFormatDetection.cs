using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Determines whether an interpolated string literal contains at least one interpolation hole
/// using the &lt;PropertyName&gt; tag format - shared by <see cref="InterpolatedStringLocalMessageAnalyzer"/>
/// (RSSL0007), <see cref="InterpolatedStringMessageExpressionAnalyzer"/> (RSSL0009), and
/// <see cref="InterpolatedStringHelperMethodArgumentAnalyzer"/> (RSSL0010). Those three diagnostics
/// only matter when a &lt;PropertyName&gt; tag is actually at stake: an interpolated string with no
/// tags that loses the interpolated-string-handler overload only loses the disabled-level
/// evaluation optimization, which isn't what these diagnostics police.
/// </summary>
internal static class InterpolatedStringTagFormatDetection
{
    /// <summary>
    /// Returns whether <paramref name="interpolatedString"/> has at least one interpolation hole
    /// whose format specifier is a non-empty <c>&lt;PropertyName&gt;</c> (or
    /// <c>&lt;@PropertyName&gt;</c>) tag.
    /// </summary>
    public static bool ContainsPropertyTag(InterpolatedStringExpressionSyntax interpolatedString)
    {
        foreach (InterpolatedStringContentSyntax content in interpolatedString.Contents)
        {
            if (content is InterpolationSyntax { FormatClause: { } formatClause } &&
                LogPropertyTagFormatParsing.TryGetPropertyName(formatClause.FormatStringToken.ValueText) is not null)
            {
                return true;
            }
        }

        return false;
    }
}
