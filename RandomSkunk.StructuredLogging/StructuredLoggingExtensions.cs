using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

using LogAttribute = (string Key, object? Value);

using LogAttributeSpanOrArray =
#if NET9_0_OR_GREATER
    Span<(string Key, object? Value)>;
#else
    (string Key, object? Value)[];
#endif

/// <summary>
/// Provides extension methods for structured logging using <see cref="ILogger"/>.
/// </summary>
/// <remarks>
/// 
/// </remarks>
public static partial class StructuredLoggingExtensions
{
    /// <summary>
    /// Writes a log entry at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="logAttributes">Key value pairs associated with the log.</param>
    public static void Write(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        string? message,
        params LogAttributeSpanOrArray logAttributes)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!logger.IsEnabled(logLevel))
            return;

#if !NET9_0_OR_GREATER
        logAttributes ??= [];
#endif

        for (int i = 0; i < logAttributes.Length; i++)
        {
            if (logAttributes[i].Key == null)
            {
#if NET9_0_OR_GREATER
                // If any key is null, we have a hole in our span. Shift all subsequent elements left by one and slice off the last element.
                logAttributes[(i + 1)..].CopyTo(logAttributes[i..]);
                logAttributes = logAttributes[..^1];
#else
                // If any key is null, we have a hole in our array. Remove all holes by filtering the array.
                logAttributes = [.. logAttributes.Where(logAttribute => !string.IsNullOrEmpty(logAttribute.Key))];
                break;
#endif
            }
        }

#if NET9_0_OR_GREATER
        switch (logAttributes.Length)
        {
            case 0: logger.Write0(logLevel, eventId, exception, message); break;
            case 1: logger.Write1(logLevel, eventId, exception, message, logAttributes); break;
            case 2: logger.Write2(logLevel, eventId, exception, message, logAttributes); break;
            case 3: logger.Write3(logLevel, eventId, exception, message, logAttributes); break;
            case 4: logger.Write4(logLevel, eventId, exception, message, logAttributes); break;
            case 5: logger.Write5(logLevel, eventId, exception, message, logAttributes); break;
            case 6: logger.Write6(logLevel, eventId, exception, message, logAttributes); break;
            case 7: logger.Write7(logLevel, eventId, exception, message, logAttributes); break;
            case 8: logger.Write8(logLevel, eventId, exception, message, logAttributes); break;
            default: logger.WriteN(logLevel, eventId, exception, message, logAttributes); break;
        }
#else
        logger.WriteN(logLevel, eventId, exception, message, logAttributes);
#endif
    }

    /// <summary>
    /// Writes a log entry at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="keyValuePairs">Key value pairs (e.g. <c>Dictionary&lt;string, object?&gt;</c>) to add to the log.</param>
    public static void Write(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        string? message,
        IReadOnlyCollection<KeyValuePair<string, object?>> keyValuePairs)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!logger.IsEnabled(logLevel))
            return;

        LogAttribute[] logAttributes;
        if (keyValuePairs?.Count > 0)
        {
            logAttributes = new LogAttribute[keyValuePairs.Count];
            int i = 0;
            foreach (KeyValuePair<string, object?> logAttribute in keyValuePairs)
                logAttributes[i++] = (logAttribute.Key, logAttribute.Value);
        }
        else
        {
            logAttributes = [];
        }

        logger.Write(logLevel, eventId, exception, message, logAttributes);
    }

    /// <summary>
    /// Begins a logging scope with the specified message and log attributes.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> instance to create the scope for. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="message">A message to associate with the scope.</param>
    /// <param name="logAttributes">A collection of attributes to include in the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the scope when disposed, or <see langword="null"/> if no scope was
    /// created, either because no message or attributes were provided, or because scopes were disabled on the logger.</returns>
    public static IDisposable? BeginScope(
        this ILogger logger,
        string message,
        params LogAttributeSpanOrArray logAttributes)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (logAttributes is not { Length: > 0 } && string.IsNullOrEmpty(message))
            return null;

#if NET9_0_OR_GREATER
        return logAttributes.Length switch
        {
            0 => logger.BeginScope0(message),
            1 => logger.BeginScope1(message, logAttributes),
            2 => logger.BeginScope2(message, logAttributes),
            3 => logger.BeginScope3(message, logAttributes),
            4 => logger.BeginScope4(message, logAttributes),
            5 => logger.BeginScope5(message, logAttributes),
            6 => logger.BeginScope6(message, logAttributes),
            7 => logger.BeginScope7(message, logAttributes),
            8 => logger.BeginScope8(message, logAttributes),
            _ => logger.BeginScopeN(message, logAttributes),
        };
#else
        return logger.BeginScopeN(message, logAttributes);
#endif
    }

    /// <summary>
    /// Begins a logging scope with the specified log attributes.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> instance to create the scope for. Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="logAttributes">A collection of attributes to include in the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the scope when disposed, or <see langword="null"/> if no scope was
    /// created, either because no attributes were provided, or because scopes were disabled on the logger.</returns>
    public static IDisposable? BeginScope(
        this ILogger logger,
        params LogAttributeSpanOrArray logAttributes) =>
        logger.BeginScope(null!, logAttributes);

    private static void WriteN(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttribute[] array = new LogAttribute[logAttributes.Length];
        logAttributes.CopyTo(array
#if !NET9_0_OR_GREATER
            .AsSpan()
#endif
            );
        LogState<LogAttributeArray> state = new(message, new LogAttributeArray(array));
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray>.Formatter);
    }

    private static IDisposable? BeginScopeN(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttribute[] array = new LogAttribute[logAttributes.Length];
        logAttributes.CopyTo(array
#if !NET9_0_OR_GREATER
            .AsSpan()
#endif
            );
        return logger.BeginScope(new LogState<LogAttributeArray>(message, new LogAttributeArray(array)));
    }

#if NET9_0_OR_GREATER
    private static void Write0(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message)
    {
        LogState<LogAttributeArray0> state = new(message, default);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray0>.Formatter);
    }

    private static void Write1(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray1 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray1> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray1>.Formatter);
    }

    private static void Write2(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray2 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray2> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray2>.Formatter);
    }

    private static void Write3(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray3 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray3> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray3>.Formatter);
    }

    private static void Write4(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray4 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray4> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray4>.Formatter);
    }

    private static void Write5(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray5 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray5> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray5>.Formatter);
    }

    private static void Write6(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray6 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray6> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray6>.Formatter);
    }

    private static void Write7(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray7 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray7> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray7>.Formatter);
    }

    private static void Write8(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray8 array = default;
        logAttributes.CopyTo(array);
        LogState<LogAttributeArray8> state = new(message, array);
        logger.Log(logLevel, eventId, state, exception, LogState<LogAttributeArray8>.Formatter);
    }

    private static IDisposable? BeginScope0(this ILogger logger, string message)
    {
        return logger.BeginScope(new LogState<LogAttributeArray0>(message, default));
    }

    private static IDisposable? BeginScope1(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray1 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray1>(message, array));
    }

    private static IDisposable? BeginScope2(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray2 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray2>(message, array));
    }

    private static IDisposable? BeginScope3(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray3 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray3>(message, array));
    }

    private static IDisposable? BeginScope4(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray4 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray4>(message, array));
    }

    private static IDisposable? BeginScope5(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray5 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray5>(message, array));
    }

    private static IDisposable? BeginScope6(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray6 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray6>(message, array));
    }

    private static IDisposable? BeginScope7(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray7 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray7>(message, array));
    }

    private static IDisposable? BeginScope8(this ILogger logger, string message, LogAttributeSpanOrArray logAttributes)
    {
        LogAttributeArray8 array = default;
        logAttributes.CopyTo(array);
        return logger.BeginScope(new LogState<LogAttributeArray8>(message, array));
    }
#endif
}
