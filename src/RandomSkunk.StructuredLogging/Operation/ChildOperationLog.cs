using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLogBase{TOperationLog}.BeginSubOperation(string)"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState._journal"/>. Applies no synchronization of its own - see
/// <see cref="SynchronizedSubOperationLog"/> for the decorator that wraps this type when an operation is
/// begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string operationName)
    : OperationLog<ISubOperationLog>(state, operationName), ISubOperationLog
{
    public ISubOperationLog AppendException(Exception exception)
    {
        _state.ThrowIfDisposed();
        _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");
        return this;
    }

    public ISubOperationLog AppendResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        StringBuilder journal = _state.BeginJournalEntry().Append($"`{_operationName}` result: ");
        ValueFormatting.AppendValue(journal, value);
        return this;
    }

    public ISubOperationLog Escalate(LogLevel level)
    {
        _state.ThrowIfDisposed();
        LogLevel previousLevel = _state.Escalate(level);
        if (level > previousLevel)
            BeginJournalEntry().Append($"`{_operationName}` escalated from {previousLevel} to {level}.");
        return this;
    }

    protected override void DisposeCore()
    {
        _state.ThrowIfDisposed();
        _state.BeginJournalEntry().Append($"`{_operationName}` complete.");
    }
}
