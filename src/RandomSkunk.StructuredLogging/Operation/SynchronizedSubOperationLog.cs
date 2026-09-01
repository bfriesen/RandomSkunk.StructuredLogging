using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Wraps a sub-operation <see cref="ISubOperationLog"/> (normally a <see cref="ChildOperationLog"/>, though
/// this decorator doesn't depend on that) so every member is synchronized on a shared <c>gate</c> - the same
/// one the root <see cref="SynchronizedOperationLog"/> it descends from uses. Every member is already
/// implemented, locked, on <see cref="SynchronizedOperationLogBase"/> (which holds the wrapped log as
/// <see cref="IOperationLogBase"/>, since <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/>
/// both extend it); this class only adds a same-named, explicit <see cref="ISubOperationLog"/> fluent
/// wrapper for each - a one-line call to the base member, returning <see langword="this"/>.
/// </summary>
internal sealed class SynchronizedSubOperationLog(ISubOperationLog inner, object gate)
    : SynchronizedOperationLogBase(inner, gate), ISubOperationLog
{
    ISubOperationLog ISubOperationLog.AddProperty<T>(string propertyName, T value)
    {
        AddProperty(propertyName, value);
        return this;
    }

    ISubOperationLog ISubOperationLog.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    ISubOperationLog ISubOperationLog.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    ISubOperationLog ISubOperationLog.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    ISubOperationLog ISubOperationLog.Append(string text)
    {
        Append(text);
        return this;
    }

    ISubOperationLog ISubOperationLog.Append(ref OperationLogInterpolatedStringHandler text)
    {
        Append(ref text);
        return this;
    }

    ISubOperationLog ISubOperationLog.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    ISubOperationLog ISubOperationLog.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }
}
