namespace RandomSkunk.StructuredLogging;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState._journal"/> or, for <see cref="SetException"/> with
/// <c>recordEverywhere: true</c>, sets <see cref="OperationLogState.Exception"/>. Applies no synchronization
/// of its own - see <see cref="SynchronizedOperationLog"/> for the decorator that wraps this type when an
/// operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string name)
    : OperationLog<ChildOperationLog>(state, name), IOperationLog
{
    public IOperationLog SetException(Exception exception, bool recordEverywhere = false)
    {
        _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");

        if (recordEverywhere)
            _state.Exception = exception;

        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        _state.BeginJournalEntry().Append($"`{_operationName}` result: {ValueFormatting.Format(value)}");
        return this;
    }

    protected override void DisposeCore()
    {
        _state.BeginJournalEntry().Append($"`{_operationName}` complete.");
    }
}
