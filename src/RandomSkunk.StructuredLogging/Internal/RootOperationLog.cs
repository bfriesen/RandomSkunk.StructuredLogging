using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel)"/>.
/// Its <see cref="Dispose"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string name) : IOperationLog
{
    private bool _disposed;

    public IOperationLog SetProperty<T>(string propertyName, T value)
    {
        lock (state)
            state.Properties.Add((propertyName, value));
        return this;
    }

    public IOperationLog Append(string text)
    {
        lock (state)
            state.AppendLine(text);
        return this;
    }

    public ISubOperationLog BeginSubOperation(string subOperationName)
    {
        lock (state)
        {
            state.StartLine();
            state.Journal.Append('`').Append(subOperationName).Append("` started.");
        }

        return new ChildOperationLog(state, subOperationName);
    }

    public IOperationLog SetException(Exception exception)
    {
        lock (state)
            state.Exception = exception;
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        lock (state)
        {
            state.Result = value;
            state.HasResult = true;
        }

        return this;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        KeyValuePair<string, object?>[] logProperties;
        Exception? exception;

        lock (state)
        {
            state.Stopwatch.Stop();

            var i = 0;
            logProperties = new KeyValuePair<string, object?>[state.Properties.Count + 3 + (state.HasResult ? 1 : 0)];
            logProperties[i++] = new("Operation.StartTime", state.StartTime);
            logProperties[i++] = new("Operation.DurationMs", state.Stopwatch.Elapsed.TotalMilliseconds);
            logProperties[i++] = new("Operation.Log", state.Journal.ToString());

            if (state.HasResult)
                logProperties[i++] = new("Operation.Result", state.Result);

            foreach (var (propertyName, propertyValue) in state.Properties)
                logProperties[i++] = new(propertyName, propertyValue);

            exception = state.Exception;

            OperationLogPools.Journals.Return(state.Journal);
            OperationLogPools.PropertyLists.Return(state.Properties);
        }

        state.Logger.Write(logProperties, state.Level, state.EventId, exception, $"Operation complete: {name}");
    }
}
