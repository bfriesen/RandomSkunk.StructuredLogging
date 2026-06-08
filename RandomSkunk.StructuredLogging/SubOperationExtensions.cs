using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines extension methods to create sub-operation logs.
/// </summary>
public static class SubOperationExtensions
{
    /// <summary>
    /// Creates a sub-operation log that immediately appends a "sub-operation starting" message to the operation log. When
    /// disposed, a "sub-operation complete" message is appended to the operation log. All other methods and properties
    /// behave the same as a regular operation log.
    /// </summary>
    /// <param name="parentOperationLog">The parent operation log.</param>
    /// <param name="subOperationName">The name of the sub-operation.</param>
    /// <returns>The sub-operation log.</returns>
    public static IOperationLog SubOperation(
        this IOperationLog? parentOperationLog,
        string subOperationName) =>
        new SubOperationLog(parentOperationLog ?? default(OperationLog<EmptyNameValuePairArray>), subOperationName);

    private class SubOperationLog : IOperationLogInternal
    {
        private readonly IOperationLog _operationLog;
        private readonly string _subOperationName;
        private readonly StringBuilder? _stringBuilder;

        public SubOperationLog(IOperationLog operationLog, string subOperationName)
        {
            _operationLog = operationLog;
            _subOperationName = subOperationName;
            _stringBuilder = (operationLog as IOperationLogInternal)?.StringBuilder;

            operationLog.Append($"Sub-operation started: {subOperationName}");
        }

        EventId IOperationLog.EventId => _operationLog.EventId;

        List<KeyValuePair<string, object?>> IOperationLog.Properties => _operationLog.Properties;

        StringBuilder? IOperationLogInternal.StringBuilder => _stringBuilder;

        void IDisposable.Dispose() =>
            _operationLog.Append($"Sub-operation complete: {_subOperationName}");

        void IOperationLog.Append(ref InterpolatedString.OperationLogEntry logEntry) =>
            _operationLog.Append(ref logEntry);

        T IOperationLog.ReturnValue<T>(T returnValue, string returnValueExpression) =>
            _operationLog.ReturnValue(returnValue, returnValueExpression);

        TException IOperationLog.Exception<TException>(TException exception, string exceptionExpression) =>
            _operationLog.Exception(exception, exceptionExpression);

        T IOperationLog.Value<T>(T value, string valueExpression) =>
            _operationLog.Value(value);

        bool IOperationLog.Condition(bool condition, string conditionExpression) =>
            _operationLog.Condition(condition, conditionExpression);

        bool IOperationLog.IsNull<T>([NotNullWhen(false)] T value, string valueExpression) =>
            _operationLog.IsNull(value, valueExpression);

        bool IOperationLog.IsNull<T>([NotNullWhen(false)] T? value, string valueExpression) =>
            _operationLog.IsNull(value, valueExpression);

        bool IOperationLog.IsNullOrEmpty([NotNullWhen(false)] string? value, string valueExpression) =>
            _operationLog.IsNullOrEmpty(value, valueExpression);

        bool IOperationLog.IsNullOrWhiteSpace([NotNullWhen(false)] string? value, string valueExpression) =>
            _operationLog.IsNullOrWhiteSpace(value, valueExpression);
    }
}