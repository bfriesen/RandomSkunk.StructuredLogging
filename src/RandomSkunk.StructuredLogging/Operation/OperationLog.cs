using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="RootOperationLog"/> and <see cref="ChildOperationLog"/>, holding the members
/// whose implementations are identical between the two. Does not implement <see cref="IOperationLogBase{TOperationLog}"/>
/// itself - <typeparamref name="TOperationLog"/> is the interface (<see cref="IOperationLog"/> for
/// <see cref="RootOperationLog"/>, <see cref="ISubOperationLog"/> for <see cref="ChildOperationLog"/>) that
/// the concrete derived class actually implements, letting these members return it directly via
/// <c>(TOperationLog)this</c> without each derived class redeclaring them.
/// </summary>
/// <typeparam name="TOperationLog">
/// The interface the most-derived type implements - <see cref="IOperationLog"/> or
/// <see cref="ISubOperationLog"/>.
/// </typeparam>
internal abstract class OperationLog<TOperationLog>(OperationLogState state, string operationName)
    : IJournalOwner
    where TOperationLog : class, IOperationLogBase<TOperationLog>
{
    protected readonly OperationLogState _state = state;
    protected readonly string _operationName = operationName;

    private int _disposed;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        _state.Properties ?? (IReadOnlyList<KeyValuePair<string, object?>>)[];

    public EventId EventId => _state.EventId;

#pragma warning disable CA1822 // Mark members as static
    public bool IsEnabled => true;
#pragma warning restore CA1822 // Mark members as static

    public TOperationLog AddProperty<T>(string propertyName, T value)
    {
        _state.ThrowIfDisposed();
        _state.AddProperty(propertyName, value);
        return (TOperationLog)(object)this;
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

    public TOperationLog Append(string text)
    {
        BeginJournalEntry().Append(text);
        return (TOperationLog)(object)this;
    }

#pragma warning disable IDE0060 // Remove unused parameter
    public TOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        // The handler already wrote everything directly into the journal (via BeginJournalEntry, in
        // its constructor) while it was being built - nothing left to do here.
        return (TOperationLog)(object)this;
    }
#pragma warning restore IDE0060 // Remove unused parameter

    public TOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendValue(journal, value);
        return (TOperationLog)(object)this;
    }

    public TOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendJson(journal, value);
        return (TOperationLog)(object)this;
    }

    public ISubOperationLog BeginSubOperation(string subOperationName)
    {
        BeginJournalEntry().Append($"`{subOperationName}` started.");
        return new ChildOperationLog(_state, subOperationName);
    }

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
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
