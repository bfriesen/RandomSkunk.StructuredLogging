using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// An <see cref="IOperationLog"/> returned when the operation's level is disabled on the logger, so a
/// disabled operation does zero journal accumulation - mirroring how the interpolated string handlers
/// skip evaluating their holes entirely when disabled. Unlike a fully no-op implementation,
/// <see cref="EventId"/> and <see cref="Properties"/> (via <see cref="AddProperty{T}"/>) still behave as
/// callers would expect from an enabled operation, since code may read them regardless of whether the
/// operation ends up writing a log entry - e.g. to tag an unrelated log line with the operation's
/// <see cref="EventId"/>, or to pass <see cref="Properties"/> to another structured log call. Every other
/// member is a no-op. A root and every sub-operation begun from it are behaviorally identical - there's no
/// per-level state left to distinguish them - so <see cref="BeginSubOperation(string)"/> just returns
/// <see langword="this"/> instead of allocating.
/// </summary>
internal sealed class DisabledOperationLog(EventId eventId) : IOperationLog
{
    private List<KeyValuePair<string, object?>>? _properties;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        _properties ?? (IReadOnlyList<KeyValuePair<string, object?>>)[];

    public EventId EventId => eventId;

    public bool IsEnabled => false;

    public ISubOperationLog AddProperty<T>(string name, T value)
    {
        (_properties ??= new(capacity: 8)).Add(new(name, value));
        return this;
    }

    public IOperationLog SetException(Exception exception) => this;

    public ISubOperationLog Escalate(LogLevel level) => this;

    public IOperationLog SetResult<T>(T value) => this;

    public ISubOperationLog AppendException(Exception exception) => this;

    public ISubOperationLog AppendResult<T>(T value) => this;

    public ISubOperationLog Append(string text) => this;

    // The handler's constructor already saw IsEnabled == false and skipped evaluating text's
    // interpolated arguments entirely - nothing was written anywhere.
    public ISubOperationLog Append(ref OperationLogInterpolatedStringHandler text) => this;

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null) => this;

    public ISubOperationLog BeginSubOperation(string operationName) => this;

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName) => this;

    public void Dispose()
    {
    }
}
