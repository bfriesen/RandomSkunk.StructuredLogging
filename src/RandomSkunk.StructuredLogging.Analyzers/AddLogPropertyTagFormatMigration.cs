using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement interpolation hole for a call flagged by <see cref="NonCapturingInterpolationHoleAnalyzer"/>
/// (RSSL0003) - see <see cref="NonCapturingInterpolationHoleCodeFixProvider"/>. Prepends a
/// <c>&lt;PropertyName&gt;</c> tag to the hole's format specifier so its value starts being
/// captured as a structured property, e.g. <c>{ts:HH:mm:ss}</c> becomes <c>{ts:&lt;PropertyName&gt;HH:mm:ss}</c>,
/// and <c>{who}</c> (no format at all) becomes <c>{who:&lt;PropertyName&gt;}</c>. If the hole already
/// had an empty destructuring tag (<c>&lt;@&gt;</c>), that destructuring status is preserved rather
/// than dropped, e.g. <c>{value:&lt;@&gt;N2}</c> becomes <c>{value:&lt;@Value&gt;N2}</c> - this
/// method never invents destructuring on a hole that didn't already request it.
/// </summary>
internal static class AddLogPropertyTagFormatMigration
{
    /// <summary>
    /// Returns the original and rewritten interpolation hole for <paramref name="hole"/>, along
    /// with the guessed property name it now captures and whether the hole was already a
    /// destructuring tag, or <see langword="null"/> if <paramref name="hole"/> already captures a
    /// structured property.
    /// </summary>
    public static (InterpolationSyntax OldHole, InterpolationSyntax NewHole, string PropertyName, bool Destructure)? TryCreateReplacement(
        InterpolationSyntax hole, SemanticModel semanticModel)
    {
        InterpolationFormatClauseSyntax? formatClause = hole.FormatClause;
        string? existingFormat = formatClause?.FormatStringToken.ValueText;

        if (existingFormat is not null && LogPropertyTagFormatParsing.TryGetPropertyName(existingFormat) is not null)
            return null;

        (string? remainingFormat, bool destructure) = AnalyzeExistingFormat(existingFormat);
        string propertyName = GuessPropertyName(hole.Expression, semanticModel);

        string tagPrefix = destructure ? $"<@{propertyName}>" : $"<{propertyName}>";
        string newFormatText = remainingFormat is null ? tagPrefix : $"{tagPrefix}{remainingFormat}";
        SyntaxToken newFormatToken = SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, newFormatText, newFormatText, default);

        InterpolationFormatClauseSyntax newFormatClause = formatClause is null
            ? SyntaxFactory.InterpolationFormatClause(SyntaxFactory.Token(SyntaxKind.ColonToken), newFormatToken)
            : formatClause.WithFormatStringToken(newFormatToken);

        InterpolationSyntax newHole = hole.WithFormatClause(newFormatClause);

        return (hole, newHole, propertyName, destructure);
    }

    /// <summary>
    /// Returns the format text that will remain once any non-capturing tag prefix (a malformed
    /// <c>&lt;...</c> with no closing <c>&gt;</c>, or the <c>&lt;&gt;</c>/<c>&lt;@&gt;</c> opt-out)
    /// is stripped from <paramref name="format"/>, along with whether that prefix was a destructuring
    /// (<c>&lt;@&gt;</c>) tag whose destructuring status should be preserved. Mirrors the runtime's
    /// own <c>TagFormat</c> parsing (RandomSkunk.StructuredLogging's LogPropertyTagFormat.cs).
    /// </summary>
    private static (string? RemainingFormat, bool Destructure) AnalyzeExistingFormat(string? format)
    {
        if (string.IsNullOrEmpty(format))
            return (null, false);

        if (format![0] != '<')
            return (format, false);

        return (LogPropertyTagFormatParsing.GetRemainingFormat(format), LogPropertyTagFormatParsing.IsDestructuring(format));
    }

    /// <summary>
    /// Guesses a property name for the value captured from <paramref name="expression"/> - see
    /// <see cref="PropertyNameGuessing.TryGuessPropertyName"/> - falling back to the generic
    /// <c>"PropertyName"</c> placeholder when nothing can be guessed, since this only ever seeds a
    /// tag the developer can rename afterward.
    /// </summary>
    private static string GuessPropertyName(ExpressionSyntax expression, SemanticModel semanticModel) =>
        PropertyNameGuessing.TryGuessPropertyName(expression, semanticModel) ?? "PropertyName";
}
