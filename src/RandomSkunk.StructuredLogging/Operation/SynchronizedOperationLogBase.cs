using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="SynchronizedOperationLog"/> and <see cref="SynchronizedSubOperationLog"/>,
/// holding the <see cref="ISubOperationLog"/> members whose implementation is identical between the two -
/// everything except <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>,
/// which only the root decorator has. Implements <see cref="ISubOperationLog"/> itself (rather than leaving
/// that to <typeparamref name="TOperationLog"/>-specific derived classes, the way <see cref="OperationLog{TOperationLog}"/>
/// does via <c>TOperationLog</c>) because every member here returns <see cref="ISubOperationLog"/> regardless of
/// whether <typeparamref name="TOperationLog"/> is <see cref="IOperationLog"/> or <see cref="ISubOperationLog"/> -
/// there's no derived-type-specific return type to recover the way <c>OperationLog{TOperationLog}</c> needs to.
/// </summary>
/// <typeparam name="TOperationLog">
/// The wrapped log's type - <see cref="IOperationLog"/> for <see cref="SynchronizedOperationLog"/>,
/// <see cref="ISubOperationLog"/> for <see cref="SynchronizedSubOperationLog"/>.
/// </typeparam>
internal abstract class SynchronizedOperationLogBase<TOperationLog>(TOperationLog inner, object gate) : ISubOperationLog
    where TOperationLog : ISubOperationLog
{
    protected readonly TOperationLog _inner = inner;
    protected readonly object _gate = gate;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties
    {
        get
        {
            lock (_gate)
                return [.. _inner.Properties];
        }
    }

    public EventId EventId => _inner.EventId;

    public bool IsEnabled => _inner.IsEnabled;

    public ISubOperationLog AddProperty<T>(string name, T value)
    {
        lock (_gate)
            _inner.AddProperty(name, value);
        return this;
    }

    public ISubOperationLog AppendException(Exception exception)
    {
        lock (_gate)
            _inner.AppendException(exception);
        return this;
    }

    public ISubOperationLog AppendResult<T>(T value)
    {
        lock (_gate)
            _inner.AppendResult(value);
        return this;
    }

    public ISubOperationLog Escalate(LogLevel level)
    {
        lock (_gate)
            _inner.Escalate(level);
        return this;
    }

    public ISubOperationLog Append(string text)
    {
        lock (_gate)
            _inner.Append(text);
        return this;
    }

    // The handler evaluated text into a private buffer, not the shared journal, precisely because it
    // can't hold `_gate` while doing so - see OperationLogInterpolatedStringHandler's doc comment. Splice
    // that buffer into the real journal here, under the lock, then return it to the pool.
    public ISubOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        if (!text.IsEnabled)
            return this;

        StringBuilder rented = text.RentedBuilder!;
        try
        {
            lock (_gate)
            {
                if (_inner is IJournalOwner owner)
                {
                    StringBuilder journal = owner.BeginJournalEntry();
                    foreach (ReadOnlyMemory<char> chunk in rented.GetChunks())
                        journal.Append(chunk.Span);
                }
                else
                {
                    _inner.Append(rented.ToString());
                }
            }
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }

        return this;
    }

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            _inner.AppendValue(value, valueName);
        return this;
    }

    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            _inner.AppendJson(value, valueName);
        return this;
    }

    public ISubOperationLog BeginSubOperation(string operationName)
    {
        ISubOperationLog subOperation;
        lock (_gate)
            subOperation = _inner.BeginSubOperation(operationName);
        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        ISubOperationLog subOperation;

        if (!operationName.IsEnabled)
        {
            // Nothing was evaluated - inner.BeginSubOperation(string) ignores its argument when inner
            // is itself disabled, so the name doesn't matter here.
            lock (_gate)
                subOperation = _inner.BeginSubOperation(string.Empty);
            return new SynchronizedSubOperationLog(subOperation, _gate);
        }

        StringBuilder rented = operationName.RentedBuilder!;
        try
        {
            string name = rented.ToString();
            lock (_gate)
                subOperation = _inner.BeginSubOperation(name);
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }

        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    public void Dispose()
    {
        lock (_gate)
            _inner.Dispose();
    }
}
