namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation, returned by <see cref="IOperationLogBase{TOperationLog}.BeginSubOperation(string)"/> on
/// either the root operation or another sub-operation. Every fluent member (declared on
/// <see cref="IOperationLogBase{TOperationLog}"/>) returns <see cref="ISubOperationLog"/> itself, so a chain of
/// calls on a sub-operation keeps returning a sub-operation - deliberately unrelated to
/// <see cref="IOperationLog"/> (neither extends the other), so <see cref="IOperationLog.SetException"/>/
/// <see cref="IOperationLog.SetResult{T}"/> can never be reached from a sub-operation reference; use
/// <see cref="IOperationLogBase{TOperationLog}.AppendException"/>/<see cref="IOperationLogBase{TOperationLog}.AppendResult{T}"/>
/// instead. A sub-operation never writes its own log entry - it only ever contributes to the root's one
/// eventual log entry. Every sub-operation shares the root operation's journal, so once the root
/// <see cref="IOperationLog"/> has been disposed, calling any member other than <c>Properties</c>,
/// <c>EventId</c>, or <c>IsEnabled</c> on a sub-operation still referenced from outside the root's
/// <c>using</c> scope throws <see cref="ObjectDisposedException"/>, rather than risking corruption of an
/// unrelated operation's journal. That includes <see cref="IDisposable.Dispose"/>: it's only safe to call
/// once per sub-operation - disposing one after the root is already disposed throws like everything else.
/// </summary>
public interface ISubOperationLog : IOperationLogBase<ISubOperationLog>;
