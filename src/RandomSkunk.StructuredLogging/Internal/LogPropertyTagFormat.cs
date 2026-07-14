using System.Collections.Concurrent;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Parses the "&lt;PropertyName&gt;format" tag syntax that lets an interpolated log message's
/// format specifier also capture the interpolated value as a structured property. An empty tag
/// ("&lt;&gt;") opts out of capturing while still allowing the remaining format text to start
/// with '&lt;'. A tag whose name starts with '@' (e.g. "&lt;@PropertyName&gt;" or "&lt;@&gt;")
/// additionally renders the value into the message using Serilog-style destructured formatting
/// (see <see cref="LogPropertyDestructuring"/>) instead of <see cref="IFormattable"/>/<see cref="object.ToString"/>
/// formatting; any format text following such a tag is parsed but ignored, since destructured
/// rendering fully replaces ordinary formatting.
/// </summary>
internal static class LogPropertyTagFormat
{
    // Interpolated string format specifiers are always compile-time literal text (and therefore
    // interned, so the same literal text is always the same string instance), so caching the
    // parse result by that instance keeps the hot path allocation-free after the first
    // occurrence of each distinct literal in the process. Unlike a cache keyed by log message
    // text, there's no risk of unbounded growth from runtime-constructed strings.
    private static readonly ConcurrentDictionary<string, TagFormat> Cache = new();

    public static TagFormat Parse(string? format)
    {
        if (string.IsNullOrEmpty(format) || format[0] != '<')
            return new TagFormat(null, format);

        return Cache.GetOrAdd(format, static f => ParseCore(f));
    }

    private static TagFormat ParseCore(string format)
    {
        ReadOnlySpan<char> span = format;
        bool destructure = span.Length > 1 && span[1] == '@';
        int tagStart = destructure ? 2 : 1;
        int closeIndex = span.Slice(tagStart).IndexOf('>');

        if (closeIndex < 0)
            return new TagFormat(null, format);

        ReadOnlySpan<char> tag = span.Slice(tagStart, closeIndex);
        ReadOnlySpan<char> remaining = span.Slice(tagStart + closeIndex + 1);

        return new TagFormat(
            tag.IsEmpty ? null : tag.ToString(),
            remaining.IsEmpty ? null : remaining.ToString(),
            destructure);
    }
}

/// <summary>
/// The result of parsing a format string for a "&lt;PropertyName&gt;format" tag: the structured
/// property name to capture the value under (or <see langword="null"/> if none), the remaining
/// format text to use when formatting the value into the message, and whether the tag requested
/// Serilog-style destructured formatting (a "&lt;@..." tag).
/// </summary>
/// <param name="PropertyName">The structured property name to capture the value under, or <see langword="null"/> if none.</param>
/// <param name="Format">
/// The remaining format text after the tag. Ignored when <paramref name="Destructure"/> is
/// <see langword="true"/>, since destructured rendering fully replaces ordinary formatting.
/// </param>
/// <param name="Destructure">Whether the tag requested Serilog-style destructured formatting (a "&lt;@..." tag).</param>
internal readonly record struct TagFormat(string? PropertyName, string? Format, bool Destructure = false);
