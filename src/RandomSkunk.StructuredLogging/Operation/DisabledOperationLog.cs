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
/// <see cref="_shared"/>), matching how a root and its sub-operations share one
/// <see cref="OperationLogState"/> when enabled.
/// </summary>
internal sealed class DisabledOperationLog : IOperationLog
{
    private readonly EventId _eventId;
    private readonly string _operationName;
    private readonly SharedState _shared;

    /// <param name="eventId">The <see cref="EventId"/> the operation was begun with.</param>
    /// <param name="operationName">The operation's name.</param>
    public DisabledOperationLog(EventId eventId, string operationName)
        : this(eventId, operationName, new SharedState())
    {
    }

    private DisabledOperationLog(EventId eventId, string operationName, SharedState shared)
    {
        _eventId = eventId;
        _operationName = operationName;
        _shared = shared;
    }

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        (IReadOnlyList<KeyValuePair<string, object?>>?)_shared.Properties ?? [];

    public EventId EventId => _eventId;

    public string OperationName => _operationName;

    public IOperationLog AddProperty<T>(string name, T value)
    {
        (_shared.Properties ??= new(capacity: 8)).Add(new(name, value));
        return this;
    }

    public IOperationLog SetException(Exception exception, bool recordEverywhere = false) => this;

    public IOperationLog SetResult<T>(T value) => this;

    public IOperationLog Append(string text) => this;

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog BeginSubOperation(string name) => new DisabledOperationLog(_eventId, name, _shared);

    public void Dispose()
    {
    }

    private sealed class SharedState
    {
        public List<KeyValuePair<string, object?>>? Properties;
    }
}
