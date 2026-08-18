using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Wraps a root <see cref="IOperationLog"/> (normally a <see cref="RootOperationLog"/>, though this
/// decorator doesn't depend on that) so every member is synchronized on a shared <paramref name="gate"/>,
/// making it safe to use the operation concurrently (e.g. sub-operations run via <c>Task.WhenAll</c>).
/// Created by <see cref="LoggerOperationExtensions"/> when an operation is begun with
/// <c>threadSafe: true</c>. <see cref="BeginSubOperation"/> wraps the resulting sub-operation in a
/// <see cref="SynchronizedSubOperationLog"/> using this same <paramref name="gate"/>, so the whole
/// operation tree - root and every nested sub-operation - synchronizes on one lock, matching the single
/// <see cref="OperationLogState"/> they all share underneath.
/// </summary>
internal sealed class SynchronizedOperationLog(IOperationLog inner, object gate) : IOperationLog
{
    public IOperationLog SetProperty<T>(string name, T value)
    {
        lock (gate)
            inner.SetProperty(name, value);
        return this;
    }

    public IOperationLog Append(string text)
    {
        lock (gate)
            inner.Append(text);
        return this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (gate)
            inner.AppendValue(value, valueName);
        return this;
    }

    public ISubOperationLog BeginSubOperation(string name)
    {
        ISubOperationLog subOperation;
        lock (gate)
            subOperation = inner.BeginSubOperation(name);
        return new SynchronizedSubOperationLog(subOperation, gate);
    }

    public IOperationLog SetException(Exception exception)
    {
        lock (gate)
            inner.SetException(exception);
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        lock (gate)
            inner.SetResult(value);
        return this;
    }

    public void Dispose()
    {
        lock (gate)
            inner.Dispose();
    }
}
