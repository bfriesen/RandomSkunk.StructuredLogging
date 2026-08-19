using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>.
/// Its <see cref="DisposeCore"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// Applies no synchronization of its own - see <see cref="SynchronizedOperationLog"/> for the decorator
/// that wraps this type when an operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string name)
    : OperationLog<RootOperationLog>(state, name), IOperationLog
{
    public IOperationLog SetException(Exception exception, bool recordEverywhere = false)
    {
        _state.Exception = exception;

        if (recordEverywhere)
        {
            _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");
        }

        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        _state.Result = value;
        _state.HasResult = true;
        return this;
    }

    protected override void DisposeCore()
    {
        _state.Stopwatch.Stop();
        string journal = _state.BeginJournalEntry()
            .Append("Operation complete.")
            .ToString();

        if (_state.HasResult)
            _state.AddProperty("Operation.Result", _state.Result);

        _state.ReturnJournalToPool();
        _state.Logger.Write(
            _state.Properties ?? [],
            _state.Level,
            _state.EventId,
            _state.Exception,
            $"Operation complete: {_operationName:<Operation.Name>}",
            ("Operation.Journal", journal),
            ("Operation.StartTime", _state.StartTime),
            ("Operation.DurationSeconds", _state.Stopwatch.Elapsed.TotalSeconds));
    }
}
