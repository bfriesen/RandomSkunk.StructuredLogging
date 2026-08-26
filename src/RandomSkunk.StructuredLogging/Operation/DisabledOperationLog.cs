using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// An <see cref="IOperationLog"/> returned when the operation's level is disabled on the logger, so a
/// disabled operation does zero journal accumulation - mirroring how the interpolated string handlers
/// skip evaluating their holes entirely when disabled. Unlike a fully no-op implementation,
/// <see cref="EventId"/>, <see cref="OperationName"/>, and <see cref="Properties"/> (via
/// <see cref="AddProperty{T}"/>) still behave as callers would expect from an enabled operation, since
/// code may read them regardless of whether the operation ends up writing a log entry - e.g. to tag an
/// unrelated log line with the operation's <see cref="EventId"/>, or to pass <see cref="Properties"/> to
/// another structured log call. Every other member is a no-op. <see cref="BeginSubOperation"/> returns a
/// new <see cref="DisabledOperationLog"/> with the sub-operation's own <see cref="OperationName"/> but
/// sharing this instance's <see cref="EventId"/> and <see cref="Properties"/> list (via
/// <see cref="_state"/>), matching how a root and its sub-operations share one
/// <see cref="OperationLogState"/> when enabled.
/// </summary>
internal sealed class DisabledOperationLog : IOperationLog
{
    private readonly string _operationName;
    private readonly State _state;

    /// <param name="eventId">The <see cref="EventId"/> the operation was begun with.</param>
    /// <param name="operationName">The operation's name.</param>
    public DisabledOperationLog(EventId eventId, string operationName)
        : this(operationName, new State(eventId))
    {
    }

    public object Gate => _state;

    private DisabledOperationLog(string operationName, State shared)
    {
        _operationName = operationName;
        _state = shared;
    }

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        (IReadOnlyList<KeyValuePair<string, object?>>?)_state.Properties ?? [];

    public EventId EventId => _state.EventId;

    public string OperationName => _operationName;

    public IOperationLog AddProperty<T>(string name, T value)
    {
        (_state.Properties ??= new(capacity: 8)).Add(new(name, value));
        return this;
    }

    public IOperationLog SetException(Exception exception) => this;

    public IOperationLog Escalate(LogLevel level) => this;

    public IOperationLog SetResult<T>(T value) => this;

    public IOperationLog Append(string text) => this;

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog BeginSubOperation(string operationName) => new DisabledOperationLog(operationName, _state);

    public void Dispose()
    {
    }

    private sealed class State(EventId eventId)
    {
        public readonly EventId EventId = eventId;

        public List<KeyValuePair<string, object?>>? Properties;
    }
}
