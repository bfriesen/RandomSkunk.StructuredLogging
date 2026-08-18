using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// The root <see cref="IOperationLog"/> returned by <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/>.
/// Its <see cref="DisposeCore"/> is the only place that ever writes to <see cref="OperationLogState.Logger"/> -
/// every <see cref="ChildOperationLog"/> nested under it only ever contributes to <see cref="OperationLogState"/>.
/// Applies no synchronization of its own - see <see cref="SynchronizedOperationLog"/> for the decorator
/// that wraps this type when an operation is begun with <c>threadSafe: true</c>.
/// </summary>
internal sealed class RootOperationLog(OperationLogState state, string name)
    : OperationLog<RootOperationLog>(state, name), IOperationLog
{
    public IOperationLog SetException(Exception exception, bool recordEverywhere = false)
    {
        _state.Exception = exception;

        if (recordEverywhere)
        {
            _state.StartLine().Append($"`{_operationName}` failed:\n{exception}");
        }

        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        _state.Result = value;
        _state.HasResult = true;
        return this;
    }

    protected override void DisposeCore()
    {
        _state.Stopwatch.Stop();
        string journal = _state.StartLine().Append($"Operation completed in {_state.Stopwatch.Elapsed.TotalSeconds:F3} seconds.").ToString();

        var i = 0;
        var logProperties = new KeyValuePair<string, object?>[_state.Properties.Count + 3 + (_state.HasResult ? 1 : 0)];
        logProperties[i++] = new("Operation.StartTime", _state.StartTime);
        logProperties[i++] = new("Operation.DurationMs", _state.Stopwatch.Elapsed.TotalMilliseconds);
        logProperties[i++] = new("Operation.Journal", journal);

        if (_state.HasResult)
            logProperties[i++] = new("Operation.Result", _state.Result);

        foreach (var (propertyName, propertyValue) in _state.Properties)
            logProperties[i++] = new(propertyName, propertyValue);

        _state.ReturnToPools();
        _state.Logger.Write(logProperties, _state.Level, _state.EventId, _state.Exception, $"Operation complete: {_operationName}");
    }
}
