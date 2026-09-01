using System.Diagnostics.CodeAnalysis;
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
/// <see cref="OperationLogState"/> they all share underneath. Every member <see cref="IOperationLog"/>
/// shares with <see cref="ISubOperationLog"/> is already implemented, locked, on
/// <see cref="SynchronizedOperationLogBase"/> (which holds the wrapped log as <see cref="IOperationLogBase"/>,
/// since both interfaces extend it); this class only adds a same-named, explicit <see cref="IOperationLog"/>
/// fluent wrapper for each - a one-line call to the base member, returning <see langword="this"/> - plus
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>, the two members <see cref="IOperationLogBase"/>
/// doesn't declare, which this class reaches through its own <see cref="IOperationLog"/>-typed
/// <see cref="_inner"/> field instead.
/// </summary>
internal sealed class SynchronizedOperationLog(IOperationLog inner, object gate)
    : SynchronizedOperationLogBase(inner, gate), IOperationLog
{
    private readonly IOperationLog _inner = inner;

    IOperationLog IOperationLog.SetException(Exception exception)
    {
        lock (_gate)
            _inner.SetException(exception);
        return this;
    }

    IOperationLog IOperationLog.SetResult<T>(T value)
    {
        lock (_gate)
            _inner.SetResult(value);
        return this;
    }

    IOperationLog IOperationLog.AddProperty<T>(string propertyName, T value)
    {
        AddProperty(propertyName, value);
        return this;
    }

    IOperationLog IOperationLog.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    IOperationLog IOperationLog.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    IOperationLog IOperationLog.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    IOperationLog IOperationLog.Append(string text)
    {
        Append(text);
        return this;
    }

    IOperationLog IOperationLog.Append(ref OperationLogInterpolatedStringHandler text)
    {
        Append(ref text);
        return this;
    }

    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }
}
