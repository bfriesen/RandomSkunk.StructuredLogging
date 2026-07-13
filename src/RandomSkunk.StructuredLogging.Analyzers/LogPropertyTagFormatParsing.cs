namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Parses the <c>&lt;PropertyName&gt;</c> tag prefix of an interpolation hole's format specifier -
/// shared by <see cref="LogPropertyTagFormatAnalyzer"/> and <see cref="LogPropertyTagFormatMigration"/>.
/// Mirrors the tag-detection half of the runtime's own parsing (RandomSkunk.StructuredLogging's
/// Internal/LogPropertyTagFormat.cs), which this netstandard2.0-only project can't reference
/// directly since it doesn't depend on that library.
/// </summary>
internal static class LogPropertyTagFormatParsing
{
    /// <summary>
    /// Returns the captured property name if <paramref name="format"/> is a non-empty
    /// <c>&lt;PropertyName&gt;...</c> tag, or <see langword="null"/> if it isn't a tag at all, or
    /// is the empty (<c>&lt;&gt;</c>) opt-out tag.
    /// </summary>
    public static string? TryGetPropertyName(string format)
    {
        if (format.Length == 0 || format[0] != '<')
            return null;

        var closeIndex = format.IndexOf('>', 1);
        if (closeIndex < 0)
            return null;

        var propertyName = format.Substring(1, closeIndex - 1);
        return propertyName.Length == 0 ? null : propertyName;
    }
}
