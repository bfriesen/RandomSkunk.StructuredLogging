using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by
/// <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>.
/// Its <see cref="DisposeCore"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// Applies no synchronization of its own - see <see cref="SynchronizedOperationLog"/> for the decorator
/// that wraps this type when an operation is begun with <c>threadSafe: true</c>. Every fluent member
/// shared with <see cref="ChildOperationLog"/> is a plain, non-self-returning <see langword="void"/> method
/// on <see cref="OperationLogBase"/>; this class's own same-named explicit <see cref="IOperationLog"/>
/// members are the ones that actually satisfy <see cref="IOperationLog"/>, each just calling the base
/// version and returning <see langword="this"/>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string operationName)
    : OperationLogBase(state, operationName), IOperationLog
{
    IOperationLog IOperationLog.AddProperty<T>(string propertyName, T value)
    {
        AddProperty(propertyName, value);
        return this;
    }

    IOperationLog IOperationLog.Append(string text)
    {
        Append(text);
        return this;
    }

    IOperationLog IOperationLog.Append(ref OperationLogInterpolatedStringHandler text)
    {
        Append(ref text);
        return this;
    }

    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }

    IOperationLog IOperationLog.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    IOperationLog IOperationLog.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    IOperationLog IOperationLog.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    IOperationLog IOperationLog.SetException(Exception exception)
    {
        _state.ThrowIfDisposed();
        BeginJournalEntry().Append(_state.Exception is not null
            ? "Operation exception set again, overwriting the previous value."
            : "Operation exception set.");
        _state.Exception = exception;
        return this;
    }

    IOperationLog IOperationLog.SetResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        BeginJournalEntry().Append(_state.HasResult
            ? "Operation result set again, overwriting the previous value."
            : "Operation result set.");
        _state.Result = value;
        _state.HasResult = true;
        return this;
    }

    public override void AppendException(Exception exception)
    {
        _state.ThrowIfDisposed();
        BeginJournalEntry().Append($"Operation failed:\n{exception}");
    }

    public override void AppendResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        StringBuilder journal = BeginJournalEntry().Append("Operation result: ");
        ValueFormatting.AppendValue(journal, value);
    }

    public override void Escalate(LogLevel level)
    {
        _state.ThrowIfDisposed();
        LogLevel previousLevel = _state.Escalate(level);
        if (level > previousLevel)
            BeginJournalEntry().Append($"Operation escalated from {previousLevel} to {level}.");
    }

    protected override void DisposeCore()
    {
        try
        {
            _state.Stopwatch.Stop();
            _state.FinalizeHeader();

            string journal = _state.BeginJournalEntry()
                .Append("Operation complete.")
                .ToString();

            // The journal header prints StartTime as local time (with its offset), for a human
            // reading the entry in their own context. Operation.StartTime is the machine-facing
            // property, so it's converted to UTC here instead - a consumer correlating entries
            // across timezones shouldn't have to parse the offset back out itself.
            if (_state.HasResult)
                _state.Logger.Write(
                    _state.Properties ?? (IReadOnlyCollection<KeyValuePair<string, object?>>)[],
                    _state.Level,
                    _state.EventId,
                    _state.Exception,
                    journal,
                    ("Operation.Name", _operationName),
                    ("Operation.StartTime", _state.StartTime.UtcDateTime),
                    ("Operation.DurationSeconds", _state.Stopwatch.Elapsed.TotalSeconds),
                    ("Operation.Result", _state.Result));
            else
                _state.Logger.Write(
                    _state.Properties ?? (IReadOnlyCollection<KeyValuePair<string, object?>>)[],
                    _state.Level,
                    _state.EventId,
                    _state.Exception,
                    journal,
                    ("Operation.Name", _operationName),
                    ("Operation.StartTime", _state.StartTime.UtcDateTime),
                    ("Operation.DurationSeconds", _state.Stopwatch.Elapsed.TotalSeconds));
        }
        finally
        {
            // In a finally so that a throw anywhere above - a sink that throws from ILogger.Log, or a
            // journal write that fails while building the entry - still hands the pooled journal back
            // and still marks the state disposed. Otherwise that StringBuilder is dropped on the floor
            // (the pool just allocates a replacement) and, worse, any sub-operation that outlived the
            // root goes on writing into a journal whose owner has already given up on it, instead of
            // throwing ObjectDisposedException like it does on every other path.
            _state.Dispose();
        }
    }
}
