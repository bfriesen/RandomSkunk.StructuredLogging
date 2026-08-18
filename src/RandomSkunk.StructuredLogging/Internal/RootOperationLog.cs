using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>.
/// Its <see cref="Dispose"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// Applies no synchronization of its own - see <see cref="SynchronizedOperationLog"/> for the decorator
/// that wraps this type when an operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string name)
    : OperationLog<RootOperationLog>(state, name), IOperationLog
{
    public IOperationLog SetException(Exception exception, bool propagateToRoot = false)
    {
        _state.Exception = exception;
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        _state.Result = value;
        _state.HasResult = true;
        return this;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _state.Stopwatch.Stop();

        var i = 0;
        var logProperties = new KeyValuePair<string, object?>[_state.Properties.Count + 3 + (_state.HasResult ? 1 : 0)];
        logProperties[i++] = new("Operation.StartTime", _state.StartTime);
        logProperties[i++] = new("Operation.DurationMs", _state.Stopwatch.Elapsed.TotalMilliseconds);
        logProperties[i++] = new("Operation.Journal", _state.Journal.ToString());

        if (_state.HasResult)
            logProperties[i++] = new("Operation.Result", _state.Result);

        foreach (var (propertyName, propertyValue) in _state.Properties)
            logProperties[i++] = new(propertyName, propertyValue);

        var exception = _state.Exception;

        OperationLogPools.Journals.Return(_state.Journal);
        OperationLogPools.PropertyLists.Return(_state.Properties);

        _state.Logger.Write(logProperties, _state.Level, _state.EventId, exception, $"Operation complete: {_operationName}");
    }
}
