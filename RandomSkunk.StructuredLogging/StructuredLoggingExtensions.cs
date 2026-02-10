using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines extension methods for structured logging using <see cref="ILogger"/>.
/// </summary>
/// <content>
/// Defines the non-public WriteStructuredLog extension methods used by the public logging methods. Each method is optimized for
/// its specific scenario to minimize allocations and maximize performance. Methods are marked internal for testing purposes.
/// </content>
public static partial class StructuredLoggingExtensions
{
    internal static void WriteStructuredLog(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        params (string Key, object? Value)[] logAttributes)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeArray>(in messageData, new(logAttributes)),
            exception,
            LogState<LogAttributeArray>.Formatter);
    }

    internal static void WriteStructuredLog<T>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T Value) logAttribute)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T>>(in messageData, new(in logAttribute)),
            exception,
            LogState<LogAttributeTuple<T>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2>>(in messageData, new(in logAttribute1, in logAttribute2)),
            exception,
            LogState<LogAttributeTuple<T1, T2>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3, T4>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3,
        ref readonly (string Key, T4 Value) logAttribute4)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3, T4>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3,
        ref readonly (string Key, T4 Value) logAttribute4,
        ref readonly (string Key, T5 Value) logAttribute5)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3,
        ref readonly (string Key, T4 Value) logAttribute4,
        ref readonly (string Key, T5 Value) logAttribute5,
        ref readonly (string Key, T6 Value) logAttribute6)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6, T7>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3,
        ref readonly (string Key, T4 Value) logAttribute4,
        ref readonly (string Key, T5 Value) logAttribute5,
        ref readonly (string Key, T6 Value) logAttribute6,
        ref readonly (string Key, T7 Value) logAttribute7)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>>.Formatter);
    }

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6, T7, T8>(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Key, T1 Value) logAttribute1,
        ref readonly (string Key, T2 Value) logAttribute2,
        ref readonly (string Key, T3 Value) logAttribute3,
        ref readonly (string Key, T4 Value) logAttribute4,
        ref readonly (string Key, T5 Value) logAttribute5,
        ref readonly (string Key, T6 Value) logAttribute6,
        ref readonly (string Key, T7 Value) logAttribute7,
        ref readonly (string Key, T8 Value) logAttribute8)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>>(in messageData, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.Formatter);
    }

    internal static void WriteStructuredLog(
        this ILogger logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        IReadOnlyCollection<KeyValuePair<string, object?>>? keyValuePairs)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.Log(
            logLevel,
            eventId,
            new LogState<KeyValuePairCollection>(in messageData, new(keyValuePairs)),
            exception,
            LogState<KeyValuePairCollection>.Formatter);
    }
}
