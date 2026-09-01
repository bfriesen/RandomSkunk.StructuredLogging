using System.Collections.Concurrent;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Parses the "&lt;PropertyName&gt;format" tag syntax that lets an interpolated log message's
/// format specifier also capture the interpolated value as a structured property. An empty tag
/// ("&lt;&gt;") opts out of capturing while still allowing the remaining format text to start
/// with '&lt;'. A tag whose name starts with '@' (e.g. "&lt;@PropertyName&gt;" or "&lt;@&gt;")
/// requests Serilog-style destructured capture (see <see cref="LogPropertyDestructuring"/>): if no
/// format follows the tag, the value is also rendered into the message using destructured
/// formatting instead of <see cref="IFormattable"/>/<see cref="object.ToString"/> formatting; if a
/// format does follow the tag, that format is honored for the message exactly like an ordinary
/// tag, and destructuring only affects how the property is captured (its name gets an '@' prefix).
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
        // format is known to start with '<' (checked by Parse). A '@' immediately after it
        // marks a destructuring tag, but that '@' is deliberately left in place rather than
        // skipped: slicing the tag starting right after '<' means a destructuring tag's name
        // (e.g. "@PropertyName") already carries its '@' prefix once `tag` is turned into a
        // string below, with no separate string concatenation needed to add it back.
        ReadOnlySpan<char> span = format;
        bool destructure = span.Length > 1 && span[1] == '@';
        int closeIndex = span[1..].IndexOf('>');

        if (closeIndex < 0)
        {
            throw new UnterminatedLogPropertyTagException(
                $"The format \"{format}\" starts with '<' but has no matching '>', so it can't be parsed as a \"<PropertyName>\" capture tag. If the format is meant to start with a literal '<', use the \"<>\" escape hatch instead, e.g. \"<>{format}\".");
        }

        ReadOnlySpan<char> tag = span.Slice(1, closeIndex);
        ReadOnlySpan<char> remaining = span[(closeIndex + 2)..];

        // An empty tag ("<>") or a bare destructuring tag with no name ("<@>") both mean "no
        // property to capture" - the latter is just "@" once sliced, since the '@' was kept
        // rather than stripped.
        return new TagFormat(
            tag.IsEmpty || (tag.Length == 1 && tag[0] == '@') ? null : tag.ToString(),
            remaining.IsEmpty ? null : remaining.ToString(),
            destructure);
    }
}

/// <summary>
/// Thrown when an interpolation hole's format specifier begins with '&lt;' but has no matching
/// '&gt;', so it cannot be parsed as either a "&lt;PropertyName&gt;format" capture tag or the
/// "&lt;&gt;" no-capture escape hatch. A real format string that needs to start with a literal
/// '&lt;' must use the "&lt;&gt;" escape (e.g. "&lt;&gt;&lt;custom&gt;") rather than leaving the
/// '&lt;' unescaped.
/// </summary>
public sealed class UnterminatedLogPropertyTagException : FormatException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnterminatedLogPropertyTagException"/> class.
    /// </summary>
    public UnterminatedLogPropertyTagException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnterminatedLogPropertyTagException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnterminatedLogPropertyTagException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnterminatedLogPropertyTagException"/> class
    /// with a specified error message and a reference to the inner exception that is the cause of
    /// this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or <see langword="null"/> if no
    /// inner exception is specified.
    /// </param>
    public UnterminatedLogPropertyTagException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// The result of parsing a format string for a "&lt;PropertyName&gt;format" tag: the structured
/// property name to capture the value under (or <see langword="null"/> if none), the remaining
/// format text to use when formatting the value into the message, and whether the tag requested
/// Serilog-style destructured formatting (a "&lt;@..." tag).
/// </summary>
/// <param name="PropertyName">
/// The structured property name to capture the value under, or <see langword="null"/> if none.
/// Already carries the '@' prefix when <paramref name="Destructure"/> is <see langword="true"/>,
/// so callers can use it as-is without re-checking <paramref name="Destructure"/>.
/// </param>
/// <param name="Format">
/// The remaining format text after the tag, or <see langword="null"/> if there is none. When
/// <paramref name="Destructure"/> is <see langword="true"/> and this is <see langword="null"/>,
/// the value is rendered into the message using destructured formatting instead. A non-null
/// <see cref="Format"/> is always honored for the message - even when <paramref name="Destructure"/>
/// is <see langword="true"/> - so destructuring then only affects how the property is captured.
/// </param>
/// <param name="Destructure">Whether the tag requested Serilog-style destructured formatting (a "&lt;@..." tag).</param>
internal readonly record struct TagFormat(string? PropertyName, string? Format, bool Destructure = false);
