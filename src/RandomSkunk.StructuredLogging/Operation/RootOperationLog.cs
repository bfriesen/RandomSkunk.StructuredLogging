using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>.
/// Its <see cref="DisposeCore"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// Applies no synchronization of its own - see <see cref="SynchronizedOperationLog"/> for the decorator
/// that wraps this type when an operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string operationName)
    : OperationLog<IOperationLog>(state, operationName), IOperationLog
{
    public IOperationLog SetException(Exception exception)
    {
        _state.ThrowIfDisposed();
        _state.Exception = exception;
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        _state.Result = value;
        _state.HasResult = true;
        return this;
    }

    public IOperationLog AppendException(Exception exception)
    {
        _state.ThrowIfDisposed();
        BeginJournalEntry().Append($"Operation failed:\n{exception}");
        return this;
    }

    public IOperationLog AppendResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        StringBuilder journal = BeginJournalEntry().Append("Operation result: ");
        ValueFormatting.AppendValue(journal, value);
        return this;
    }

    public IOperationLog Escalate(LogLevel level)
    {
        _state.ThrowIfDisposed();
        LogLevel previousLevel = _state.Escalate(level);
        if (level > previousLevel)
            BeginJournalEntry().Append($"Operation escalated from {previousLevel} to {level}.");
        return this;
    }

    protected override void DisposeCore()
    {
        _state.Stopwatch.Stop();

        string journal = _state.BeginJournalEntry()
            .Append("Operation complete.")
            .ToString();
        _state.Dispose();

        if (_state.HasResult)
            _state.AddProperty("Operation.Result", _state.Result);

        _state.Logger.Write(
            _state.Properties ?? (IReadOnlyCollection<KeyValuePair<string, object?>>)[],
            _state.Level,
            _state.EventId,
            _state.Exception,
            journal,
            ("Operation.Name", _operationName),
            ("Operation.StartTime", _state.StartTime),
            ("Operation.DurationSeconds", _state.Stopwatch.Elapsed.TotalSeconds));
    }
}
