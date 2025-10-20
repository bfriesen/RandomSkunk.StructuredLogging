using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// An interpolated string optimized for <c>ILogger.Write</c> extension methods. Given the specified <c>logger</c> and
/// <c>logLevel</c> parameters, the interpolated string will only be evaluated if the logger is enabled at that log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct InterpolatedString
{
    private readonly bool _isEnabled;
    private DefaultInterpolatedStringHandler _defaultInterpolatedStringHandler;

    /// <summary>Initializes a new instance of the <see cref="InterpolatedString"/> struct.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public InterpolatedString(int literalLength, int formattedCount, ILogger? logger, LogLevel logLevel, out bool isEnabled)
    {
        _isEnabled = isEnabled = (literalLength > 0 || formattedCount > 0) && logger?.IsEnabled(logLevel) == true;
        _defaultInterpolatedStringHandler = isEnabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value) => _defaultInterpolatedStringHandler.AppendFormatted(value);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="format">The format string.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => _defaultInterpolatedStringHandler.AppendFormatted(value, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, int alignment) => _defaultInterpolatedStringHandler.AppendFormatted(value, alignment);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="format">The format string.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, int alignment, string? format) => _defaultInterpolatedStringHandler.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified character span to the handler.</summary>
    /// <param name="value">The span to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value) => _defaultInterpolatedStringHandler.AppendFormatted(value);

    /// <summary>Writes the specified string of chars to the handler.</summary>
    /// <param name="value">The span to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _defaultInterpolatedStringHandler.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? value) => _defaultInterpolatedStringHandler.AppendFormatted(value);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _defaultInterpolatedStringHandler.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _defaultInterpolatedStringHandler.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified string to the handler.</summary>
    /// <param name="value">The string to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendLiteral(string value) => _defaultInterpolatedStringHandler.AppendLiteral(value);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    /// <returns>The built string.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString() =>
        _isEnabled
            ? _defaultInterpolatedStringHandler.ToString()
            : null;

    /// <summary>Gets the built <see cref="string"/> and clears the handler.</summary>
    /// <returns>The built string.</returns>
    /// <remarks>
    /// This releases any resources used by the handler. The method should be invoked only
    /// once and as the last thing performed on the handler. Subsequent use is erroneous, ill-defined,
    /// and may destabilize the process, as may using any other copies of the handler after ToStringAndClear
    /// is called on any one of them.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? ToStringAndClear() =>
        _isEnabled
            ? _defaultInterpolatedStringHandler.ToStringAndClear()
            : null;
}

/// <summary>
/// An interpolated string optimized for <c>ILogger</c>.<c><typeparamref name="TLogLevel"/></c> extension methods. Given the
/// specified <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the
/// <typeparamref name="TLogLevel"/> log level.
/// </summary>
[InterpolatedStringHandler]
public ref struct InterpolatedString<TLogLevel> where TLogLevel : struct, ILogLevel
{
    private static readonly LogLevel _logLevel = default(TLogLevel).LogLevel;

    private InterpolatedString _interpolatedString;

    /// <summary>Initializes a new instance of the <see cref="InterpolatedString"/> struct.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public InterpolatedString(int literalLength, int formattedCount, ILogger? logger, out bool isEnabled) =>
        _interpolatedString = new InterpolatedString(literalLength, formattedCount, logger, _logLevel, out isEnabled);

    /// <summary>
    /// Converts an <see cref="InterpolatedString{TLogLevel}"/> to an <see cref="InterpolatedString"/>.
    /// </summary>
    public static implicit operator InterpolatedString(InterpolatedString<TLogLevel> message) => message._interpolatedString;

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value) => _interpolatedString.AppendFormatted(value);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="format">The format string.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, string? format) => _interpolatedString.AppendFormatted(value, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, int alignment) => _interpolatedString.AppendFormatted(value, alignment);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="format">The format string.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <typeparam name="T">The type of the value to write.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted<T>(T value, int alignment, string? format) => _interpolatedString.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified character span to the handler.</summary>
    /// <param name="value">The span to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value) => _interpolatedString.AppendFormatted(value);

    /// <summary>Writes the specified string of chars to the handler.</summary>
    /// <param name="value">The span to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _interpolatedString.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? value) => _interpolatedString.AppendFormatted(value);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _interpolatedString.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified value to the handler.</summary>
    /// <param name="value">The value to write.</param>
    /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it
    /// indicates left-aligned and the required minimum is the absolute value.</param>
    /// <param name="format">The format string.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _interpolatedString.AppendFormatted(value, alignment, format);

    /// <summary>Writes the specified string to the handler.</summary>
    /// <param name="value">The string to write.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AppendLiteral(string value) => _interpolatedString.AppendLiteral(value);

    /// <summary>Gets the built <see cref="string"/>.</summary>
    /// <returns>The built string.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString() => _interpolatedString.ToString();

    /// <summary>Gets the built <see cref="string"/> and clears the handler.</summary>
    /// <returns>The built string.</returns>
    /// <remarks>
    /// This releases any resources used by the handler. The method should be invoked only
    /// once and as the last thing performed on the handler. Subsequent use is erroneous, ill-defined,
    /// and may destabilize the process, as may using any other copies of the handler after ToStringAndClear
    /// is called on any one of them.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? ToStringAndClear() => _interpolatedString.ToStringAndClear();
}
