using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

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
    protected int _disposed;

    public IOperationLog SetProperty<T>(string propertyName, T value)
    {
        _state.Properties.Add((propertyName, value));
        return (TSelf)this;
    }

    public IOperationLog Append(string text)
    {
        _state.AppendLine(text);
        return (TSelf)this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        _state.StartLine();
        _state.Journal.Append('`').Append(valueName).Append("`: ").Append(ValueFormatting.Format(value));
        return (TSelf)this;
    }

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        _state.StartLine();
        _state.Journal.Append('`').Append(valueName).Append("`: ");
        ValueFormatting.AppendJson(_state.Journal, value);
        return (TSelf)this;
    }

    public IOperationLog BeginSubOperation(string subOperationName)
    {
        _state.StartLine();
        _state.Journal.Append('`').Append(subOperationName).Append("` started.");

        return new ChildOperationLog(_state, subOperationName);
    }
}
