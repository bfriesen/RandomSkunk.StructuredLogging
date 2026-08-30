using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// An <see cref="IOperationLog"/> returned when the operation's level is disabled on the logger, so a
/// disabled operation does zero journal accumulation - mirroring how the interpolated string handlers
/// skip evaluating their holes entirely when disabled. Unlike a fully no-op implementation,
/// <see cref="EventId"/> and <see cref="Properties"/> (via <see cref="AddPropertyCore{T}"/>) still
/// behave as callers would expect from an enabled operation, since code may read them regardless of whether
/// the operation ends up writing a log entry - e.g. to tag an unrelated log line with the operation's
/// <see cref="EventId"/>, or to pass <see cref="Properties"/> to another structured log call. Every other
/// member is a no-op. A root and every sub-operation begun from it are behaviorally identical - there's no
/// per-level state left to distinguish them - so <see cref="BeginSubOperation(string)"/> just returns
/// <see langword="this"/> instead of allocating. Not a singleton (unlike the type it replaced) since it
/// still carries per-operation state (<see cref="EventId"/>/<see cref="Properties"/>).
/// <para>
/// Implements both <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> directly - the two are
/// unrelated interfaces (see <see cref="IOperationLogBase{TOperationLog}"/>), but a single disabled instance still
/// needs to satisfy both, since <see cref="BeginSubOperation(string)"/> hands itself back out as an
/// <see cref="ISubOperationLog"/>. The members the two interfaces share (all except
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>) are inherited from
/// <see cref="IOperationLogBase{TOperationLog}"/> and return different types per closed instantiation
/// (<see cref="IOperationLog"/> vs. <see cref="ISubOperationLog"/>), so a single public method can't satisfy
/// both - those are implemented explicitly, qualified by the closed generic interface that actually
/// declares each member (<c>IOperationLogBase&lt;IOperationLog&gt;</c>/<c>IOperationLogBase&lt;ISubOperationLog&gt;</c>,
/// not <see cref="IOperationLog"/>/<see cref="ISubOperationLog"/> themselves, since C# requires an explicit
/// interface implementation to name the interface that declares the member), each just forwarding to a
/// shared private helper where there's real logic (<see cref="AddPropertyCore{T}"/>) or just returning
/// <see langword="this"/> where there isn't.
/// </para>
/// </summary>
internal sealed class DisabledOperationLog(EventId eventId) : IOperationLog, ISubOperationLog
{
    private List<KeyValuePair<string, object?>>? _properties;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        _properties ?? (IReadOnlyList<KeyValuePair<string, object?>>)[];

    public EventId EventId => eventId;

    public bool IsEnabled => false;

    // Validates name even though a disabled operation journals nothing, so the same call throws the
    // same way whether or not the level happens to be enabled - otherwise a null name is a latent bug
    // that only surfaces once someone turns the level on.
    private void AddPropertyCore<T>(string name, T value)
    {
        ArgumentNullException.ThrowIfNull(name);
        (_properties ??= new(capacity: 8)).Add(new(name, value));
    }

    IOperationLog IOperationLogBase<IOperationLog>.AddProperty<T>(string name, T value) { AddPropertyCore(name, value); return this; }

    ISubOperationLog IOperationLogBase<ISubOperationLog>.AddProperty<T>(string name, T value) { AddPropertyCore(name, value); return this; }

    public IOperationLog SetException(Exception exception) => this;

    IOperationLog IOperationLogBase<IOperationLog>.Escalate(LogLevel level) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.Escalate(LogLevel level) => this;

    public IOperationLog SetResult<T>(T value) => this;

    IOperationLog IOperationLogBase<IOperationLog>.AppendException(Exception exception) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendException(Exception exception) => this;

    IOperationLog IOperationLogBase<IOperationLog>.AppendResult<T>(T value) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendResult<T>(T value) => this;

    IOperationLog IOperationLogBase<IOperationLog>.Append(string text) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.Append(string text) => this;

    // The handler's constructor already saw IsEnabled == false and skipped evaluating text's
    // interpolated arguments entirely - nothing was written anywhere.
    IOperationLog IOperationLogBase<IOperationLog>.Append(ref OperationLogInterpolatedStringHandler text) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.Append(ref OperationLogInterpolatedStringHandler text) => this;

    IOperationLog IOperationLogBase<IOperationLog>.AppendValue<T>(T value, string? valueName) => this;

    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendValue<T>(T value, string? valueName) => this;

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    IOperationLog IOperationLogBase<IOperationLog>.AppendJson<T>(T value, string? valueName) => this;

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendJson<T>(T value, string? valueName) => this;

    public ISubOperationLog BeginSubOperation(string operationName) => this;

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName) => this;

    public void Dispose()
    {
    }
}
