namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Parses the <c>&lt;PropertyName&gt;</c> tag prefix of an interpolation hole's format specifier -
/// shared by <see cref="LogPropertyTagFormatAnalyzer"/> and <see cref="LogPropertyTagFormatMigration"/>.
/// Mirrors the tag-detection half of the runtime's own parsing (RandomSkunk.StructuredLogging's
/// Internal/LogPropertyTagFormat.cs), which this netstandard2.0-only project can't reference
/// directly since it doesn't depend on that library. A tag whose name starts with '@' (e.g.
/// <c>&lt;@PropertyName&gt;</c> or <c>&lt;@&gt;</c>) requests Serilog-style destructured message
/// formatting - see <see cref="IsDestructuring"/>.
/// </summary>
internal static class LogPropertyTagFormatParsing
{
    /// <summary>
    /// Returns the captured property name if <paramref name="format"/> is a non-empty
    /// <c>&lt;PropertyName&gt;...</c> or <c>&lt;@PropertyName&gt;...</c> tag, or
    /// <see langword="null"/> if it isn't a tag at all, or is the empty (<c>&lt;&gt;</c> or
    /// <c>&lt;@&gt;</c>) opt-out tag.
    /// </summary>
    public static string? TryGetPropertyName(string format)
    {
        if (format.Length == 0 || format[0] != '<')
            return null;

        var closeIndex = format.IndexOf('>', 1);
        if (closeIndex < 0)
            return null;

        var nameStart = format.Length > 1 && format[1] == '@' ? 2 : 1;
        var propertyName = format.Substring(nameStart, closeIndex - nameStart);
        return propertyName.Length == 0 ? null : propertyName;
    }

    /// <summary>
    /// Returns whether <paramref name="format"/> is a <c>&lt;@...&gt;</c> tag - one whose name
    /// starts with '@', requesting Serilog-style destructured message formatting.
    /// </summary>
    public static bool IsDestructuring(string format) =>
        format.Length > 1 && format[0] == '<' && format[1] == '@' && format.IndexOf('>', 1) >= 0;

    /// <summary>
    /// Returns the format text remaining after the <c>&lt;PropertyName&gt;</c> tag, or
    /// <see langword="null"/> if nothing follows the tag. Only meaningful when
    /// <see cref="TryGetPropertyName"/> already returned non-<see langword="null"/> for
    /// <paramref name="format"/> (so it's known to start with '&lt;' and contain a matching '&gt;').
    /// </summary>
    public static string? GetRemainingFormat(string format)
    {
        var closeIndex = format.IndexOf('>', 1);
        var remainingFormat = format.Substring(closeIndex + 1);
        return remainingFormat.Length == 0 ? null : remainingFormat;
    }
}
