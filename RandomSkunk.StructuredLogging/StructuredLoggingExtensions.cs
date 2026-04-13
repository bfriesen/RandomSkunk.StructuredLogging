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
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        params (string Name, object? Value)[] logProperties) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyArray>(in messageData, new(logProperties)),
            exception,
            LogState<LogPropertyArray>.Formatter);

    internal static void WriteStructuredLog<T>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T Value) logProperty) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T>>(in messageData, new(in logProperty)),
            exception,
            LogState<LogPropertyTuple<T>>.Formatter);

    internal static void WriteStructuredLog<T1, T2>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2>>(in messageData, new(in logProperty1, in logProperty2)),
            exception,
            LogState<LogPropertyTuple<T1, T2>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3, T4>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3, T4>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3, in logProperty4)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3, T4>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3, T4, T5>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3, T4, T5>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6, T7>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6,
        ref readonly (string Name, T7 Value) logProperty7) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>>.Formatter);

    internal static void WriteStructuredLog<T1, T2, T3, T4, T5, T6, T7, T8>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6,
        ref readonly (string Name, T7 Value) logProperty7,
        ref readonly (string Name, T8 Value) logProperty8) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>>(in messageData, new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8)),
            exception,
            LogState<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>>.Formatter);

    internal static void WriteStructuredLog(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        IReadOnlyCollection<KeyValuePair<string, object?>>? logProperties) =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<ReadOnlyNameValuePairCollection>(in messageData, new(logProperties)),
            exception,
            LogState<ReadOnlyNameValuePairCollection>.Formatter);

    internal static void WriteStructuredLog<TNameValuePairList>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        MessageData messageData,
        TNameValuePairList? logProperties)
        where TNameValuePairList : IReadOnlyList<KeyValuePair<string, object?>> =>
        logger?.Log(
            logLevel,
            eventId,
            new LogState<ReadOnlyNameValuePairList<TNameValuePairList>>(in messageData, new(logProperties)),
            exception,
            LogState<ReadOnlyNameValuePairList<TNameValuePairList>>.Formatter);
}
