using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// An <see cref="IOperationLog"/> returned when the operation's level is disabled on the logger, so a
/// disabled operation does zero journal accumulation - mirroring how the interpolated string handlers
/// skip evaluating their holes entirely when disabled. Unlike a fully no-op implementation, <see cref="EventId"/>
/// and <see cref="Properties"/> (via <see cref="AddProperty{T}"/>) still behave as callers would expect
/// from an enabled operation, since code may read them regardless of whether the operation ends up
/// writing a log entry - e.g. to tag an unrelated log line with the operation's <see cref="EventId"/>, or
/// to pass <see cref="Properties"/> to another structured log call. Every other member is a no-op, and
/// <see cref="BeginSubOperation"/> returns <see langword="this"/>, so a sub-operation shares the same
/// <see cref="EventId"/> and <see cref="Properties"/> list as its ancestors, matching how a root and its
/// sub-operations share one <see cref="OperationLogState"/> when enabled.
/// </summary>
/// <param name="eventId">The <see cref="EventId"/> the operation was begun with.</param>
internal sealed class DisabledOperationLog(EventId eventId) : IOperationLog
{
    private List<KeyValuePair<string, object?>>? _properties;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        (IReadOnlyList<KeyValuePair<string, object?>>?)_properties ?? [];

    public EventId EventId => eventId;

    public IOperationLog AddProperty<T>(string name, T value)
    {
        (_properties ??= new(capacity: 8)).Add(new(name, value));
        return this;
    }

    public IOperationLog SetException(Exception exception, bool recordEverywhere = false) => this;

    public IOperationLog SetResult<T>(T value) => this;

    public IOperationLog Append(string text) => this;

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public IOperationLog BeginSubOperation(string name) => this;

    public void Dispose()
    {
    }
}
