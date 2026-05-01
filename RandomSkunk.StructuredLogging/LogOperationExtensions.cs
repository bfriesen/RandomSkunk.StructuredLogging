using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Provides extension methods for writing structured logs that track the start and completion of operations.
/// </summary>
public static partial class LogOperationExtensions
{
    internal static OperationLog<EmptyNameValuePairArray> LogOperation(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new());

    internal static OperationLog<LogPropertyTuple<T>> LogOperation<T>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string Name, T Value) logProperty) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty));

    internal static OperationLog<LogPropertyTuple<T1, T2>> LogOperation<T1, T2>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3>> LogOperation<T1, T2, T3>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3, T4>> LogOperation<T1, T2, T3, T4>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3,
        ref readonly (string, T4) logProperty4) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3, T4, T5>> LogOperation<T1, T2, T3, T4, T5>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3,
        ref readonly (string, T4) logProperty4,
        ref readonly (string, T5) logProperty5) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3, T4, T5, T6>> LogOperation<T1, T2, T3, T4, T5, T6>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3,
        ref readonly (string, T4) logProperty4,
        ref readonly (string, T5) logProperty5,
        ref readonly (string, T6) logProperty6) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>> LogOperation<T1, T2, T3, T4, T5, T6, T7>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3,
        ref readonly (string, T4) logProperty4,
        ref readonly (string, T5) logProperty5,
        ref readonly (string, T6) logProperty6,
        ref readonly (string, T7) logProperty7) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7));

    internal static OperationLog<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>> LogOperation<T1, T2, T3, T4, T5, T6, T7, T8>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName,
        ref readonly (string, T1) logProperty1,
        ref readonly (string, T2) logProperty2,
        ref readonly (string, T3) logProperty3,
        ref readonly (string, T4) logProperty4,
        ref readonly (string, T5) logProperty5,
        ref readonly (string, T6) logProperty6,
        ref readonly (string, T7) logProperty7,
        ref readonly (string, T8) logProperty8) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8));
}
