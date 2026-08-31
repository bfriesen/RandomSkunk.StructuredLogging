using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps the root <see cref="IOperationLog"/> (normally a <see cref="RootOperationLog"/>, though this
/// decorator doesn't depend on that) so every member is synchronized on a shared <c>gate</c>, making it
/// safe to use the operation concurrently (e.g. sub-operations run via <c>Task.WhenAll</c>). Created by
/// <see cref="LoggerOperationExtensions"/> when an operation is begun with <c>threadSafe: true</c>.
/// <see cref="SynchronizedOperationLogBase.BeginSubOperation(string)"/> wraps the resulting sub-operation in
/// a <see cref="SynchronizedSubOperationLog"/> using this same gate, so the whole operation tree - root and
/// every nested sub-operation - synchronizes on one lock, matching the single
/// <see cref="OperationLogState"/> they all share underneath. The <c>...Core</c> methods below are the only
/// thing <see cref="SynchronizedOperationLogBase"/> needs from this class - each makes the one call to
/// <see cref="_inner"/> that the base class's locking wrapper can't make itself, since
/// <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> share no common interface to type a
/// wrapped-log field as. Adds <see cref="SetException"/>/<see cref="SetResult{T}"/> to the members
/// <see cref="SynchronizedOperationLogBase"/> already provides - the two members that only
/// <see cref="IOperationLog"/> declares, not <see cref="ISubOperationLog"/>.
/// </summary>
internal sealed class SynchronizedOperationLog(IOperationLog inner, object gate)
    : SynchronizedOperationLogBase(gate), IOperationLog
{
    private readonly IOperationLog _inner = inner;

    public override EventId EventId => _inner.EventId;

    public override bool IsEnabled => _inner.IsEnabled;

    protected override IReadOnlyList<KeyValuePair<string, object?>> PropertiesCore() => [.. _inner.Properties];

    protected override void AddPropertyCore<T>(string name, T value) => _inner.AddProperty(name, value);

    protected override void AppendExceptionCore(Exception exception) => _inner.AppendException(exception);

    protected override void AppendResultCore<T>(T value) => _inner.AppendResult(value);

    protected override void EscalateCore(LogLevel level) => _inner.Escalate(level);

    protected override void AppendCore(string text) => _inner.Append(text);

    protected override IJournalOwner? InnerJournalOwnerCore() => _inner as IJournalOwner;

    protected override void AppendValueCore<T>(T value, string? valueName) => _inner.AppendValue(value, valueName);

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    protected override void AppendJsonCore<T>(T value, string? valueName) => _inner.AppendJson(value, valueName);

    protected override ISubOperationLog BeginSubOperationCore(string operationName) => _inner.BeginSubOperation(operationName);

    protected override void DisposeCore() => _inner.Dispose();

    public IOperationLog SetException(Exception exception)
    {
        lock (_gate)
            _inner.SetException(exception);
        return this;
    }

    public IOperationLog SetResult<T>(T value)
    {
        lock (_gate)
            _inner.SetResult(value);
        return this;
    }

    public IOperationLog AddProperty<T>(string name, T value)
    {
        base.AddPropertyLocked(name, value);
        return this;
    }

    public IOperationLog AppendException(Exception exception)
    {
        base.AppendExceptionLocked(exception);
        return this;
    }

    public IOperationLog AppendResult<T>(T value)
    {
        base.AppendResultLocked(value);
        return this;
    }

    public IOperationLog Escalate(LogLevel level)
    {
        base.EscalateLocked(level);
        return this;
    }

    public IOperationLog Append(string text)
    {
        base.AppendLocked(text);
        return this;
    }

    public IOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        base.AppendLocked(ref text);
        return this;
    }

    public IOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendValueLocked(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public IOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendJsonLocked(value, valueName);
        return this;
    }
}
