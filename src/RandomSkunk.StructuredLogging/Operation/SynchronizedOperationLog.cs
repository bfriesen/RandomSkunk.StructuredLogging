using System.Runtime.CompilerServices;
using System.Text;
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

    public bool IsEnabled => inner.IsEnabled;

    public IOperationLog SetException(Exception exception)
    {
        lock (gate)
            inner.SetException(exception);
        return this;
    }

    public IOperationLog AddProperty<T>(string name, T value)
    {
        lock (gate)
            inner.AddProperty(name, value);
        return this;
    }

    public IOperationLog Escalate(LogLevel level)
    {
        lock (gate)
            inner.Escalate(level);
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

    // The handler evaluated text into a private buffer, not the shared journal, precisely because it
    // can't hold `gate` while doing so - see OperationLogInterpolatedStringHandler's doc comment. Splice
    // that buffer into the real journal here, under the lock, then return it to the pool.
    public IOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        if (!text.IsEnabled)
            return this;

        StringBuilder rented = text.RentedBuilder!;
        try
        {
            lock (gate)
            {
                if (inner is IJournalOwner owner)
                {
                    StringBuilder journal = owner.BeginJournalEntry();
                    foreach (ReadOnlyMemory<char> chunk in rented.GetChunks())
                        journal.Append(chunk.Span);
                }
                else
                {
                    inner.Append(rented.ToString());
                }
            }
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }

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

    public IOperationLog BeginSubOperation(string operationName)
    {
        IOperationLog subOperation;
        lock (gate)
            subOperation = inner.BeginSubOperation(operationName);
        return new SynchronizedOperationLog(subOperation, gate);
    }

    public IOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        IOperationLog subOperation;

        if (!operationName.IsEnabled)
        {
            // Nothing was evaluated - inner.BeginSubOperation(string) ignores its argument when inner
            // is itself disabled, so the name doesn't matter here.
            lock (gate)
                subOperation = inner.BeginSubOperation(string.Empty);
            return new SynchronizedOperationLog(subOperation, gate);
        }

        StringBuilder rented = operationName.RentedBuilder!;
        try
        {
            string name = rented.ToString();
            lock (gate)
                subOperation = inner.BeginSubOperation(name);
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }

        return new SynchronizedOperationLog(subOperation, gate);
    }

    public void Dispose()
    {
        lock (gate)
            inner.Dispose();
    }
}
