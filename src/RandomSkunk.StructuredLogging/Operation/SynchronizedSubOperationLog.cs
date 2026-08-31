using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps a sub-operation <see cref="ISubOperationLog"/> (normally a <see cref="ChildOperationLog"/>, though
/// this decorator doesn't depend on that) so every member is synchronized on a shared <c>gate</c> - the same
/// one the root <see cref="SynchronizedOperationLog"/> it descends from uses. See
/// <see cref="SynchronizedOperationLogBase"/> for the shared locking logic; the <c>...Core</c> methods below
/// are the one call each makes to this decorator's own strongly-typed <see cref="_inner"/>, since
/// <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> share no common interface the base class
/// could hold a wrapped-log field as.
/// </summary>
internal sealed class SynchronizedSubOperationLog(ISubOperationLog inner, object gate)
    : SynchronizedOperationLogBase(gate), ISubOperationLog
{
    private readonly ISubOperationLog _inner = inner;

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

    public ISubOperationLog AddProperty<T>(string name, T value)
    {
        base.AddPropertyLocked(name, value);
        return this;
    }

    public ISubOperationLog AppendException(Exception exception)
    {
        base.AppendExceptionLocked(exception);
        return this;
    }

    public ISubOperationLog AppendResult<T>(T value)
    {
        base.AppendResultLocked(value);
        return this;
    }

    public ISubOperationLog Escalate(LogLevel level)
    {
        base.EscalateLocked(level);
        return this;
    }

    public ISubOperationLog Append(string text)
    {
        base.AppendLocked(text);
        return this;
    }

    public ISubOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        base.AppendLocked(ref text);
        return this;
    }

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendValueLocked(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendJsonLocked(value, valueName);
        return this;
    }
}
