using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Provides extension methods for writing structured logs that track the start and completion of operations, including support
/// for logging operation parameters and results.
/// </summary>
/// <remarks>These extension methods enable consistent, structured logging of operations using an ILogger instance. Each method
/// returns an IOperationLog object that logs the completion of the operation when disposed, and allows the result of the
/// operation to be included in the completion log. The methods support various overloads for specifying operation names,
/// parameters, and custom message formats. This approach helps correlate start and end log entries for operations, and is useful
/// for diagnostics and performance tracking.</remarks>
public static partial class LogOperationExtensions
{
    /// <returns>An <see cref="Operation{TNameValuePairList}"/> object that, when disposed, writes a structured log at the debug
    /// level indicating that the operation is complete. To include the result of the operation in this log, call the object's
    /// <see cref="Operation{TNameValuePairList}.Return"/> method before disposing it.</returns>
    internal static Operation<EmptyNameValuePairArray> GetOperation(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        string? operationName) =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new());

    internal static Operation<LogPropertyTuple<T>> GetOperation<T>(
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

    internal static Operation<LogPropertyTuple<T1, T2>> GetOperation<T1, T2>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3>> GetOperation<T1, T2, T3>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3, T4>> GetOperation<T1, T2, T3, T4>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3, T4, T5>> GetOperation<T1, T2, T3, T4, T5>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3, T4, T5, T6>> GetOperation<T1, T2, T3, T4, T5, T6>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>> GetOperation<T1, T2, T3, T4, T5, T6, T7>(
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

    internal static Operation<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>> GetOperation<T1, T2, T3, T4, T5, T6, T7, T8>(
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

    internal static Operation<ReadOnlyNameValuePairList<TNameValuePairList>> GetOperation<TNameValuePairList>(
        this ILogger? logger,
        LogLevel logLevel,
        EventId eventId,
        TNameValuePairList nameValuePairList,
        string? operationName)
        where TNameValuePairList : notnull, IReadOnlyList<KeyValuePair<string, object?>> =>
        new(logger,
            logLevel,
            eventId,
            operationName,
            new(nameValuePairList));
}
