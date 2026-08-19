using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps an <see cref="IOperationLog"/> (root or sub-operation - normally a <see cref="RootOperationLog"/>
/// or <see cref="ChildOperationLog"/>, though this decorator doesn't depend on that) so every member is
/// synchronized on a shared <paramref name="gate"/>, making it safe to use the operation concurrently (e.g.
/// sub-operations run via <c>Task.WhenAll</c>). Created by <see cref="LoggerOperationExtensions"/> when an
/// operation is begun with <c>threadSafe: true</c>. <see cref="BeginSubOperation"/> wraps the resulting
/// sub-operation in another <see cref="SynchronizedOperationLog"/> using this same <paramref name="gate"/>,
/// so the whole operation tree - root and every nested sub-operation - synchronizes on one lock, matching
/// the single <see cref="OperationLogState"/> they all share underneath.
/// </summary>
internal sealed class SynchronizedOperationLog(IOperationLog inner, object gate) : IOperationLog
{
    public IReadOnlyList<KeyValuePair<string, object?>> Properties
    {
        get
        {
            lock (gate)
                return [.. inner.Properties];
        }
    }

    public EventId EventId => inner.EventId;

    public IOperationLog SetException(Exception exception, bool recordEverywhere = false)
    {
        lock (gate)
            inner.SetException(exception, recordEverywhere);
        return this;
    }

    public IOperationLog AddProperty<T>(string name, T value)
    {
        lock (gate)
            inner.AddProperty(name, value);
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        lock (gate)
            inner.SetResult(value);
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

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (gate)
            inner.AppendJson(value, valueName);
        return this;
    }

    public IOperationLog BeginSubOperation(string name)
    {
        IOperationLog subOperation;
        lock (gate)
            subOperation = inner.BeginSubOperation(name);
        return new SynchronizedOperationLog(subOperation, gate);
    }

    public void Dispose()
    {
        lock (gate)
            inner.Dispose();
    }
}
