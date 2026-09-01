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
/// unrelated to each other (neither extends the other), but a single disabled instance still needs to
/// satisfy both, since <see cref="BeginSubOperation(string)"/> hands itself back out as an
/// <see cref="ISubOperationLog"/>. The members the two interfaces share (all except
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>) return different types
/// per interface (<see cref="IOperationLog"/> vs. <see cref="ISubOperationLog"/>), so a single public method
/// can't satisfy both - those are implemented explicitly, qualified by whichever interface declares each
/// member, each just forwarding to a shared private helper where there's real logic
/// (<see cref="AddPropertyCore{T}"/>) or just returning <see langword="this"/> where there isn't. Both
/// interfaces also extend the shared <see cref="IOperationLogBase"/>, whose same-named members are
/// <see langword="void"/> rather than self-returning - implementing <see cref="IOperationLog"/>/
/// <see cref="ISubOperationLog"/> means implementing that too, so this class has a third,
/// <see langword="void"/>-returning explicit implementation of each shared member alongside the two
/// self-returning ones - one implementation, not two, since both parent interfaces contribute the same
/// <see cref="IOperationLogBase"/> requirement.
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
    private void AddPropertyCore<T>(string propertyName, T value)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        (_properties ??= new(capacity: 8)).Add(new(propertyName, value));
    }

    IOperationLog IOperationLog.AddProperty<T>(string propertyName, T value) { AddPropertyCore(propertyName, value); return this; }

    ISubOperationLog ISubOperationLog.AddProperty<T>(string propertyName, T value) { AddPropertyCore(propertyName, value); return this; }

    // IOperationLog and ISubOperationLog now both extend IOperationLogBase, so a type implementing either
    // must also implement IOperationLogBase's own void-returning members - one implementation covers the
    // requirement contributed by both parent interfaces, since it's the same interface member either way.
    void IOperationLogBase.AddProperty<T>(string propertyName, T value) => AddPropertyCore(propertyName, value);

    public IOperationLog SetException(Exception exception) => this;

    IOperationLog IOperationLog.Escalate(LogLevel level) => this;

    ISubOperationLog ISubOperationLog.Escalate(LogLevel level) => this;

    void IOperationLogBase.Escalate(LogLevel level)
    {
    }

    public IOperationLog SetResult<T>(T value) => this;

    IOperationLog IOperationLog.AppendException(Exception exception) => this;

    ISubOperationLog ISubOperationLog.AppendException(Exception exception) => this;

    void IOperationLogBase.AppendException(Exception exception)
    {
    }

    IOperationLog IOperationLog.AppendResult<T>(T value) => this;

    ISubOperationLog ISubOperationLog.AppendResult<T>(T value) => this;

    void IOperationLogBase.AppendResult<T>(T value)
    {
    }

    IOperationLog IOperationLog.Append(string text) => this;

    ISubOperationLog ISubOperationLog.Append(string text) => this;

    void IOperationLogBase.Append(string text)
    {
    }

    // The handler's constructor already saw IsEnabled == false and skipped evaluating text's
    // interpolated arguments entirely - nothing was written anywhere.
    IOperationLog IOperationLog.Append(ref OperationLogInterpolatedStringHandler text) => this;

    ISubOperationLog ISubOperationLog.Append(ref OperationLogInterpolatedStringHandler text) => this;

    void IOperationLogBase.Append(ref OperationLogInterpolatedStringHandler text)
    {
    }

    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName) => this;

    ISubOperationLog ISubOperationLog.AppendValue<T>(T value, string? valueName) => this;

    void IOperationLogBase.AppendValue<T>(T value, string? valueName)
    {
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName) => this;

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    ISubOperationLog ISubOperationLog.AppendJson<T>(T value, string? valueName) => this;

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    void IOperationLogBase.AppendJson<T>(T value, string? valueName)
    {
    }

    public ISubOperationLog BeginSubOperation(string operationName) => this;

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName) => this;

    public void Dispose()
    {
    }
}
