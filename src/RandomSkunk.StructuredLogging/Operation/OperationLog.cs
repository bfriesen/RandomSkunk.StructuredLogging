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
    where TSelf : OperationLog<TSelf>, IOperationLog
{
    protected readonly OperationLogState _state = state;
    protected readonly string _operationName = operationName;
    
    private int _disposed;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties => _state.Properties ?? [];

    public EventId EventId => _state.EventId;

    public IOperationLog AddProperty<T>(string propertyName, T value)
    {
        _state.AddProperty(propertyName, value);
        return (TSelf)this;
    }

    public IOperationLog Append(string text)
    {
        _state.BeginJournalEntry().Append(text);
        return (TSelf)this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        _state.BeginJournalEntry().Append($"`{valueName}`: {ValueFormatting.Format(value)}");
        return (TSelf)this;
    }

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = _state.BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendJson(journal, value);
        return (TSelf)this;
    }

    public IOperationLog BeginSubOperation(string subOperationName)
    {
        _state.BeginJournalEntry().Append($"`{subOperationName}` started.");
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
