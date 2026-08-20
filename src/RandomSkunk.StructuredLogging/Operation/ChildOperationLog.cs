namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState._journal"/>. Applies no synchronization of its own - see
/// <see cref="SynchronizedOperationLog"/> for the decorator that wraps this type when an operation is
/// begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string operationName)
    : OperationLog<ChildOperationLog>(state, operationName), IOperationLog
{
    public IOperationLog SetException(Exception exception)
    {
        _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");
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
