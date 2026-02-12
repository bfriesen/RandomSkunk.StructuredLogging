using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RandomSkunk.StructuredLogging;

using static EditorBrowsableState;

#pragma warning disable CS1591
/// <summary>
/// An interpolated string handler for the <c>ILogger.Write</c> extension methods. Given the specified <c>logger</c> and
/// <c>logLevel</c> parameters, the interpolated string will only be evaluated if the logger is enabled at that log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct WriteInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public WriteInterpolatedStringHandler(int literalLength, int formattedCount, ILogger? logger, LogLevel logLevel, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, logLevel, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Trace</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the trace log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct TraceInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public TraceInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Trace, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Debug</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the debug log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct DebugInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public DebugInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Debug, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Information</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the information log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct InformationInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public InformationInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Information, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Warning</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the warning log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct WarningInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public WarningInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Warning, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Error</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the error log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct ErrorInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public ErrorInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Error, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

/// <summary>
/// An interpolated string handler for the <c>ILogger</c>.<c>Critical</c> extension methods. Given the specified <c>logger</c>
/// parameter, the interpolated string will only be evaluated if the logger is enabled at the critical log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct CriticalInterpolatedStringHandler
{
    private LoggingInterpolatedStringHandler _innerHandler;

    [EditorBrowsable(Never)]
    public CriticalInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
        _innerHandler = new LoggingInterpolatedStringHandler(literalLength, formattedCount, logger, LogLevel.Critical, out isEnabled);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, string? format) => _innerHandler.AppendFormatted(value, format);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);
    [EditorBrowsable(Never)] public void AppendFormatted<T>(T value, int alignment, string? format) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);
    [EditorBrowsable(Never)] public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _innerHandler.AppendFormatted(value, alignment, format);
    [EditorBrowsable(Never)] public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);
    internal MessageData GetMessageDataAndClear() => _innerHandler.GetMessageDataAndClear();
}

[StructLayout(LayoutKind.Auto)]
internal ref partial struct LoggingInterpolatedStringHandler
{
    private bool _isEnabled;
    private DefaultInterpolatedStringHandler _innerHandler;
    private NameValuePairList4 _interpolationNameValuePairs;

    public LoggingInterpolatedStringHandler(int literalLength, int formattedCount, ILogger? logger, LogLevel logLevel, out bool isEnabled)
    {
        _isEnabled = isEnabled = logger != null && logger.IsEnabled(logLevel);
        _innerHandler = isEnabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture) : default;
    }

    /// <summary>Gets the built <see cref="string"/>.</summary>
    public override string ToString() => _innerHandler.ToString();

    public void AppendFormatted<T>(T value) => _innerHandler.AppendFormatted(value);

    public void AppendFormatted<T>(T value, string? format)
    {
        if (TryParseLeadingHtmlTag(ref format, out string? key))
            _interpolationNameValuePairs.Add(new(key, value));

        _innerHandler.AppendFormatted(value, format);
    }

    public void AppendFormatted<T>(T value, int alignment) => _innerHandler.AppendFormatted(value, alignment);

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        if (TryParseLeadingHtmlTag(ref format, out string? key))
            _interpolationNameValuePairs.Add(new(key, value));

        _innerHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(scoped ReadOnlySpan<char> value) => _innerHandler.AppendFormatted(value);

    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        if (TryParseLeadingHtmlTag(ref format, out string? key))
            _interpolationNameValuePairs.Add(new(key, value.ToString()));

        _innerHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(string? value) => _innerHandler.AppendFormatted(value);

    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
    {
        if (TryParseLeadingHtmlTag(ref format, out string? key))
            _interpolationNameValuePairs.Add(new(key, value));

        _innerHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
    {
        if (TryParseLeadingHtmlTag(ref format, out string? key))
            _interpolationNameValuePairs.Add(new(key, value));

        _innerHandler.AppendFormatted(value, alignment, format);
    }

    public void AppendLiteral(string value) => _innerHandler.AppendLiteral(value);

    public MessageData GetMessageDataAndClear()
    {
        if (_isEnabled)
        {
            MessageData messageData = new(_innerHandler.ToStringAndClear(), in _interpolationNameValuePairs);
            this = default; // defensive clear
            return messageData;
        }

        return default;
    }

    private static bool TryParseLeadingHtmlTag(ref string? format, [NotNullWhen(true)] out string? logPropertyKey)
    {
        // Try to match an html tag, e.g. "<Key>", at the start of the format string
        Regex.ValueMatchEnumerator htmlTagMatch = HtmlTagRegex().EnumerateMatches(format.AsSpan());
        if (!htmlTagMatch.MoveNext())
        {
            logPropertyKey = null;
            return false;
        }

        Debug.Assert(format != null, $"A null format string should not be able to successfully match {nameof(HtmlTagRegex)}");

        int htmlTagLength = htmlTagMatch.Current.Length;
        bool hasLogAttributteKey = htmlTagLength > 2; // An empty tag has a length of 2: "<>"
        bool hasFormatAfterTag = format.Length > htmlTagLength;

        // Extract the log property key from the tag
        logPropertyKey =
            hasLogAttributteKey
                ? format[1..(htmlTagLength - 1)] // Exclude the two angle brackets to get the key
                : null; // We don't actually have a log property key when we have an empty tag

        // Remove the tag from the by-ref format parameter
        format =
            hasFormatAfterTag
                ? format[htmlTagLength..] // Keep everything after the tag
                : null; // Clear the format if nothing follows the tag

        return hasLogAttributteKey;
    }

    // This regex matches an HTML-like tag at the start of a string.
    // The tag can be empty (i.e., "<>") or contain alphanumeric characters and certain
    // special characters: - . / : [ \ ] _ |
    // Examples of valid tags: <Key>, <User-Id>, <data[0].value>, <path/file>
    [GeneratedRegex(@"^<[-./0-9:A-Z\[\\\]_a-z|]*>")]
    private static partial Regex HtmlTagRegex();
}
#pragma warning restore CS1591
