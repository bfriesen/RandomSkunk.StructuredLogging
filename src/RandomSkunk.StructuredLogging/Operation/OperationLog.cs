using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="RootOperationLog"/> and <see cref="ChildOperationLog"/>, holding the members
/// whose implementations are identical between the two. Does not implement <see cref="IOperationLog"/>
/// itself - <typeparamref name="TSelf"/> lets these members return the concrete derived type (which does
/// implement it) without each derived class having to redeclare them.
/// </summary>
/// <typeparam name="TSelf">The most-derived type, which implements <see cref="IOperationLog"/>.</typeparam>
internal abstract class OperationLog<TSelf>(OperationLogState state, string operationName)
    : IJournalOwner
    where TSelf : OperationLog<TSelf>, IOperationLog
{
    protected readonly OperationLogState _state = state;
    protected readonly string _operationName = operationName;
    
    private int _disposed;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties => _state.Properties ?? [];

    public EventId EventId => _state.EventId;

    public bool IsEnabled => true;

    public IOperationLog AddProperty<T>(string propertyName, T value)
    {
        _state.ThrowIfDisposed();
        _state.AddProperty(propertyName, value);
        return (TSelf)this;
    }

    public IOperationLog Escalate(LogLevel level)
    {
        _state.ThrowIfDisposed();
        _state.Escalate(level);
        return (TSelf)this;
    }

    /// <summary>
    /// Throws if the root operation has been disposed, then appends the "[elapsed] " timestamp prefix
    /// that starts every journal line - see <see cref="IJournalOwner"/>.
    /// </summary>
    public StringBuilder BeginJournalEntry()
    {
        _state.ThrowIfDisposed();
        return _state.BeginJournalEntry();
    }

    public IOperationLog Append(string text)
    {
        BeginJournalEntry().Append(text);
        return (TSelf)this;
    }

    public IOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        // The handler already wrote everything directly into the journal (via BeginJournalEntry, in
        // its constructor) while it was being built - nothing left to do here.
        return (TSelf)this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        BeginJournalEntry().Append($"`{valueName}`: {ValueFormatting.Format(value)}");
        return (TSelf)this;
    }

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendJson(journal, value);
        return (TSelf)this;
    }

    public IOperationLog BeginSubOperation(string subOperationName)
    {
        BeginJournalEntry().Append($"`{subOperationName}` started.");
        return new ChildOperationLog(_state, subOperationName);
    }

    public IOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        if (operationName.DirectTarget is not StringBuilder journal)
        {
            // Can't happen for RootOperationLog/ChildOperationLog - both are IJournalOwner, so the
            // handler always writes directly into the real journal - but fall back safely rather than
            // assume it, in case that ever changes.
            string fallbackName = operationName.RentedBuilder?.ToString() ?? string.Empty;
            if (operationName.RentedBuilder is StringBuilder rented)
                OperationLogPools.Journals.Return(rented);
            return BeginSubOperation(fallbackName);
        }

        int start = operationName.DirectStartIndex;
        string subOperationName = journal.ToString(start, journal.Length - start);
        journal.Insert(start, '`').Append("` started.");
        return new ChildOperationLog(_state, subOperationName);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, -1) == -1)
            return;

        DisposeCore();
    }

    protected abstract void DisposeCore();
}
