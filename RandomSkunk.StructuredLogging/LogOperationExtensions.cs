using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Provides extension methods for writing structured logs that track the start and completion of operations.
/// </summary>
public static partial class LogOperationExtensions
{
    /// <summary>
    /// A type to be used as the first optional parameter of the generic <c>LogOperation</c> extension methods. Without these
    /// parameters, when the last generic argument is type <see cref="string"/>, the compiler would choose the wrong overload.
    /// </summary>
    public struct ParameterMarker;

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
        (string Name, T Value) logProperty) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3,
        (string, T4) logProperty4) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3,
        (string, T4) logProperty4,
        (string, T5) logProperty5) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3,
        (string, T4) logProperty4,
        (string, T5) logProperty5,
        (string, T6) logProperty6) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3,
        (string, T4) logProperty4,
        (string, T5) logProperty5,
        (string, T6) logProperty6,
        (string, T7) logProperty7) =>
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
        (string, T1) logProperty1,
        (string, T2) logProperty2,
        (string, T3) logProperty3,
        (string, T4) logProperty4,
        (string, T5) logProperty5,
        (string, T6) logProperty6,
        (string, T7) logProperty7,
        (string, T8) logProperty8) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(in logProperty1, in logProperty2, in logProperty3, in logProperty4, in logProperty5, in logProperty6, in logProperty7, in logProperty8));
}
