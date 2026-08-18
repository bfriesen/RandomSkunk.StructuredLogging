using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState.Journal"/> or, for <see cref="SetException"/> with
/// <c>propagateToRoot: true</c>, sets <see cref="OperationLogState.Exception"/>. Applies no synchronization
/// of its own - see <see cref="SynchronizedOperationLog"/> for the decorator that wraps this type when an
/// operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string name) : IOperationLog
{
    private bool _disposed;

    public IOperationLog SetException(Exception exception, bool propagateToRoot = false)
    {
        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` failed:").Append('\n').Append(exception.ToString());

        if (propagateToRoot)
            state.Exception = exception;

        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` result: ").Append(ValueFormatting.Format(value));
        return this;
    }

    public IOperationLog SetProperty<T>(string propertyName, T value)
    {
        state.Properties.Add((propertyName, value));
        return this;
    }

    public IOperationLog Append(string text)
    {
        state.AppendLine(text);
        return this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        state.StartLine();
        state.Journal.Append('`').Append(valueName).Append("`: ").Append(ValueFormatting.Format(value));
        return this;
    }

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        state.StartLine();
        state.Journal.Append('`').Append(valueName).Append("`: ");
        ValueFormatting.AppendJson(state.Journal, value);
        return this;
    }

    public IOperationLog BeginSubOperation(string subOperationName)
    {
        state.StartLine();
        state.Journal.Append('`').Append(subOperationName).Append("` started.");

        return new ChildOperationLog(state, subOperationName);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` complete.");
    }
}
