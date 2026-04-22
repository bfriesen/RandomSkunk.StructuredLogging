using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace RandomSkunk.StructuredLogging;

using static EditorBrowsableState;

#pragma warning disable CS1591
/// <summary>
/// Defines various interpolated string handlers.
/// </summary>
public static partial class InterpolatedString
{
    private static readonly CompositeFormat _logEntryTimestampFormat = CompositeFormat.Parse("[{0:HH':'mm':'ss'.'fffK}] ");

    [InterpolatedStringHandler]
    public struct OperationLogEntry<TNameValuePairList>
        where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
    {
        internal StringBuilder.AppendInterpolatedStringHandler _innerHandler;

        [EditorBrowsable(Never)]
        public OperationLogEntry(int literalLength, int formattedCount, OperationLog<TNameValuePairList> operationLog, out bool isEnabled)
        {
            if (operationLog.StringBuilder is StringBuilder sb)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, _logEntryTimestampFormat, DateTime.UtcNow);

                _innerHandler = new(literalLength, formattedCount, sb, CultureInfo.InvariantCulture);
                isEnabled = true;
            }
            else
            {
                isEnabled = false;
            }
        }

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
    }

    [InterpolatedStringHandler]
    public struct OperationLogEntry
    {
        internal StringBuilder.AppendInterpolatedStringHandler _innerHandler;

        [EditorBrowsable(Never)]
        public OperationLogEntry(int literalLength, int formattedCount, IOperationLog operationLog, out bool isEnabled)
        {
            if (operationLog is IOperationLogInternal operationLogInternal && operationLogInternal.StringBuilder is StringBuilder sb)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, _logEntryTimestampFormat, DateTime.UtcNow);

                _innerHandler = new(literalLength, formattedCount, sb, CultureInfo.InvariantCulture);
                isEnabled = true;
            }
            else
            {
                isEnabled = false;
            }
        }

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
    }

    /// <summary>
    /// An interpolated string handler for the <c>ILogger.LogOperation</c> extension methods' operation name parameter. Given the
    /// specified <c>logger</c> and <c>logLevel</c> parameters, the interpolated string will only be evaluated if the logger is
    /// enabled at that log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct OperationName
    {
        internal bool _isEnabled;

        private DefaultInterpolatedStringHandler _innerHandler;

        [EditorBrowsable(Never)]
        public OperationName(int literalLength, int formattedCount, ILogger? logger, LogLevel logLevel, out bool isEnabled)
        {
            _isEnabled = isEnabled = logger != null && logger.IsEnabled(logLevel);
            if (isEnabled)
                _innerHandler = new(literalLength, formattedCount, CultureInfo.InvariantCulture);
        }

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

        internal string? ToStringAndClear()
        {
            if (_isEnabled)
            {
                string operationName = _innerHandler.ToStringAndClear();
                this = default;
                return operationName;
            }

            return null;
        }
    }

    /// <summary>
    /// An interpolated string handler for the <c>ILogger.LogOperation</c> extension methods' operation name parameter. Given the
    /// specified <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the debug
    /// log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct DebugOperationName
    {
        internal OperationName _innerHandler;

        [EditorBrowsable(Never)]
        public DebugOperationName(int literalLength, int formattedCount, ILogger? logger, out bool isEnabled) =>
            _innerHandler = new OperationName(literalLength, formattedCount, logger, LogLevel.Debug, out isEnabled);

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
    }

    /// <summary>
    /// An interpolated string handler for the <c>ILogger.Write</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> and <c>logLevel</c> parameters, the interpolated string will only be evaluated if the logger is enabled at
    /// that log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct Message
    {
        private bool _isEnabled;
        private DefaultInterpolatedStringHandler _innerHandler;
        private NameValuePairList2 _interpolationNameValuePairs;

        [EditorBrowsable(Never)]
        public Message(int literalLength, int formattedCount, ILogger? logger, LogLevel logLevel, out bool isEnabled)
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

        internal MessageData GetMessageDataAndClear()
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
            if (format is not { Length: >= 2 } || format[0] != '<')
            {
                logPropertyKey = null;
                return false;
            }

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
    }

    /// <summary>
    /// An interpolated string handler for the <c>ILogger.Trace</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the trace log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct TraceMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public TraceMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Trace, out isEnabled);

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
    /// An interpolated string handler for the <c>ILogger.Debug</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the debug log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct DebugMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public DebugMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Debug, out isEnabled);

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
    /// An interpolated string handler for the <c>ILogger.Information</c> extension methods' message parameter. Given the
    /// specified <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the
    /// information log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct InformationMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public InformationMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Information, out isEnabled);

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
    /// An interpolated string handler for the <c>ILogger.Warning</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the warning log
    /// level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct WarningMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public WarningMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Warning, out isEnabled);

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
    /// An interpolated string handler for the <c>ILogger.Error</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the error log level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct ErrorMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public ErrorMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Error, out isEnabled);

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
    /// An interpolated string handler for the <c>ILogger.Critical</c> extension methods' message parameter. Given the specified
    /// <c>logger</c> parameter, the interpolated string will only be evaluated if the logger is enabled at the critical log
    /// level.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct CriticalMessage
    {
        private Message _innerHandler;

        [EditorBrowsable(Never)]
        public CriticalMessage(int literalLength, int formattedCount, ILogger logger, out bool isEnabled) =>
            _innerHandler = new Message(literalLength, formattedCount, logger, LogLevel.Critical, out isEnabled);

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

    // This regex matches an HTML-like tag at the start of a string.
    // The tag can be empty (i.e., "<>") or contain alphanumeric characters and certain
    // special characters: - . / : [ \ ] _ |
    // Examples of valid tags: <Key>, <User-Id>, <data[0].value>, <path/file>
    [GeneratedRegex(@"^<[-./0-9:A-Z\[\\\]_a-z|]*>")]
    private static partial Regex HtmlTagRegex();
}
#pragma warning restore CS1591
