using System.Globalization;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation"/> (on either the root
/// operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState.Journal"/> or, for <see cref="SetException(Exception, bool)"/>
/// with <c>propagateToRoot: true</c>, sets <see cref="OperationLogState.Exception"/>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string name) : ISubOperationLog
{
    private bool _disposed;

    public ISubOperationLog SetProperty<T>(string propertyName, T value)
    {
        lock (state)
            state.Properties.Add((propertyName, value));
        return this;
    }

    IOperationLog IOperationLog.SetProperty<T>(string name, T value) => SetProperty(name, value);

    public ISubOperationLog Append(string text)
    {
        lock (state)
            state.AppendLine(text);
        return this;
    }

    IOperationLog IOperationLog.Append(string text) => Append(text);

    public ISubOperationLog BeginSubOperation(string subOperationName)
    {
        lock (state)
        {
            state.StartLine();
            state.Journal.Append('`').Append(subOperationName).Append("` started.");
        }

        return new ChildOperationLog(state, subOperationName);
    }

    public ISubOperationLog SetException(Exception exception) => SetException(exception, propagateToRoot: false);

    IOperationLog IOperationLog.SetException(Exception exception) => SetException(exception);

    public ISubOperationLog SetException(Exception exception, bool propagateToRoot)
    {
        lock (state)
        {
            state.StartLine();
            state.Journal.Append('`').Append(name).Append("` failed: ").Append(exception.Message);

            if (propagateToRoot)
                state.Exception = exception;
        }

        return this;
    }

    public ISubOperationLog SetResult<T>(T value)
    {
        lock (state)
        {
            state.StartLine();
            state.Journal.Append('`').Append(name).Append("` result: ").Append(Format(value));
        }

        return this;
    }

    IOperationLog IOperationLog.SetResult<T>(T value) => SetResult(value);

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (state)
        {
            state.StartLine();
            state.Journal.Append('`').Append(name).Append("` complete.");
        }
    }

    private static string Format<T>(T value) => value switch
    {
        null => "null",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };
}
