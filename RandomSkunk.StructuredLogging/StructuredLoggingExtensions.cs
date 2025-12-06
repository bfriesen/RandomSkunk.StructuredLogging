using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Provides extension methods for structured logging using <see cref="ILogger"/>.
/// </summary>
public static partial class StructuredLoggingExtensions
{
    // Marked internal for testing.
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
            new LogState<LogAttributeArray>(messageData.Message, new(logAttributes, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeArray>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T>>(messageData.Message, new(in logAttribute, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2>>(messageData.Message, new(in logAttribute1, in logAttribute2, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3, T4>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>>(messageData.Message, new(in logAttribute1, in logAttribute2, in logAttribute3, in logAttribute4, in logAttribute5, in logAttribute6, in logAttribute7, in logAttribute8, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.Formatter);
    }

    // Marked internal for testing.
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
            new LogState<KeyValuePairCollection>(messageData.Message, new(keyValuePairs, messageData.InterpolationKeyValuePairs)),
            exception,
            LogState<KeyValuePairCollection>.Formatter);
    }
}
