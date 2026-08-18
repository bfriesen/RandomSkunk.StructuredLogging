using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState.Journal"/> or, for <see cref="SetException(Exception, bool)"/>
/// with <c>propagateToRoot: true</c>, sets <see cref="OperationLogState.Exception"/>. Applies no
/// synchronization of its own - see <see cref="SynchronizedSubOperationLog"/> for the decorator that wraps
/// this type when an operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string name) : ISubOperationLog
{
    private bool _disposed;

    public ISubOperationLog SetProperty<T>(string propertyName, T value)
    {
        state.Properties.Add((propertyName, value));
        return this;
    }

    IOperationLog IOperationLog.SetProperty<T>(string name, T value) => SetProperty(name, value);

    public ISubOperationLog Append(string text)
    {
        state.AppendLine(text);
        return this;
    }

    IOperationLog IOperationLog.Append(string text) => Append(text);

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        state.StartLine();
        state.Journal.Append('`').Append(valueName).Append("`: ").Append(ValueFormatting.Format(value));
        return this;
    }

    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName) => AppendValue(value, valueName);

    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        state.StartLine();
        state.Journal.Append('`').Append(valueName).Append("`: ");
        ValueFormatting.AppendJson(state.Journal, value);
        return this;
    }

    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName) => AppendJson(value, valueName);

    public ISubOperationLog BeginSubOperation(string subOperationName)
    {
        state.StartLine();
        state.Journal.Append('`').Append(subOperationName).Append("` started.");

        return new ChildOperationLog(state, subOperationName);
    }

    public ISubOperationLog SetException(Exception exception) => SetException(exception, propagateToRoot: false);

    IOperationLog IOperationLog.SetException(Exception exception) => SetException(exception);

    public ISubOperationLog SetException(Exception exception, bool propagateToRoot)
    {
        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` failed:").Append('\n').Append(exception.ToString());

        if (propagateToRoot)
            state.Exception = exception;

        return this;
    }

    public ISubOperationLog SetResult<T>(T value)
    {
        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` result: ").Append(ValueFormatting.Format(value));
        return this;
    }

    IOperationLog IOperationLog.SetResult<T>(T value) => SetResult(value);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        state.StartLine();
        state.Journal.Append('`').Append(name).Append("` complete.");
    }
}
