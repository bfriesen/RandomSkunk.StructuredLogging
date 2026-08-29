namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps the root <see cref="IOperationLog"/> (normally a <see cref="RootOperationLog"/>, though this
/// decorator doesn't depend on that) so every member is synchronized on a shared <c>gate</c>, making it
/// safe to use the operation concurrently (e.g. sub-operations run via <c>Task.WhenAll</c>). Created by
/// <see cref="LoggerOperationExtensions"/> when an operation is begun with <c>threadSafe: true</c>.
/// <see cref="SynchronizedOperationLogBase{TOperationLog}.BeginSubOperation(string)"/> wraps the resulting
/// sub-operation in a <see cref="SynchronizedSubOperationLog"/> using this same gate, so the whole operation
/// tree - root and every nested sub-operation - synchronizes on one lock, matching the single
/// <see cref="OperationLogState"/> they all share underneath. Adds <see cref="SetException"/>/
/// <see cref="SetResult{T}"/> to the members <see cref="SynchronizedOperationLogBase{TOperationLog}"/> already
/// provides - the two members that only <see cref="IOperationLog"/> declares, not
/// <see cref="ISubOperationLog"/>.
/// </summary>
internal sealed class SynchronizedOperationLog(IOperationLog inner, object gate)
    : SynchronizedOperationLogBase<IOperationLog>(inner, gate), IOperationLog
{
    public IOperationLog SetException(Exception exception)
    {
        lock (_gate)
            _inner.SetException(exception);
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        lock (_gate)
            _inner.SetResult(value);
        return this;
    }
}
