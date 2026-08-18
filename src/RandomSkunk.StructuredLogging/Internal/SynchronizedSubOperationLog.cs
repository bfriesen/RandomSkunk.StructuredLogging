using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Wraps a sub-operation <see cref="ISubOperationLog"/> (normally a <see cref="ChildOperationLog"/>,
/// though this decorator doesn't depend on that) so every member is synchronized on a shared
/// <paramref name="gate"/> - the same one shared by the <see cref="SynchronizedOperationLog"/> at the root
/// of the operation tree (and every other <see cref="SynchronizedSubOperationLog"/> nested under it), so
/// the whole tree synchronizes on one lock, matching the single <see cref="OperationLogState"/> they all
/// share underneath. See <see cref="SynchronizedOperationLog"/>, this decorator's root-level counterpart.
/// </summary>
internal sealed class SynchronizedSubOperationLog(ISubOperationLog inner, object gate) : ISubOperationLog
{
    public ISubOperationLog SetProperty<T>(string name, T value)
    {
        lock (gate)
            inner.SetProperty(name, value);
        return this;
    }

    IOperationLog IOperationLog.SetProperty<T>(string name, T value) => SetProperty(name, value);

    public ISubOperationLog Append(string text)
    {
        lock (gate)
            inner.Append(text);
        return this;
    }

    IOperationLog IOperationLog.Append(string text) => Append(text);

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (gate)
            inner.AppendValue(value, valueName);
        return this;
    }

    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName) => AppendValue(value, valueName);

    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (gate)
            inner.AppendJson(value, valueName);
        return this;
    }

    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName) => AppendJson(value, valueName);

    public ISubOperationLog BeginSubOperation(string name)
    {
        ISubOperationLog subOperation;
        lock (gate)
            subOperation = inner.BeginSubOperation(name);
        return new SynchronizedSubOperationLog(subOperation, gate);
    }

    public ISubOperationLog SetException(Exception exception) => SetException(exception, propagateToRoot: false);

    IOperationLog IOperationLog.SetException(Exception exception) => SetException(exception);

    public ISubOperationLog SetException(Exception exception, bool propagateToRoot)
    {
        lock (gate)
            inner.SetException(exception, propagateToRoot);
        return this;
    }

    public ISubOperationLog SetResult<T>(T value)
    {
        lock (gate)
            inner.SetResult(value);
        return this;
    }

    IOperationLog IOperationLog.SetResult<T>(T value) => SetResult(value);

    public void Dispose()
    {
        lock (gate)
            inner.Dispose();
    }
}
