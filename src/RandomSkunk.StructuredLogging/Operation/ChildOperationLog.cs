using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLogBase.BeginSubOperation(string)"/> (on either the
/// root operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState._journal"/>. Applies no synchronization of its own - see
/// <see cref="SynchronizedSubOperationLog"/> for the decorator that wraps this type when an operation is
/// begun with <c>threadSafe: true</c>. Every fluent member shared with <see cref="RootOperationLog"/> is a
/// plain, non-self-returning <see langword="void"/> method on <see cref="OperationLogBase"/>; this class's
/// own same-named explicit <see cref="ISubOperationLog"/> members are the ones that actually satisfy
/// <see cref="ISubOperationLog"/>, each just calling the base version and returning <see langword="this"/>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string operationName)
    : OperationLogBase(state, operationName), ISubOperationLog
{
    ISubOperationLog ISubOperationLog.AddProperty<T>(string propertyName, T value)
    {
        AddProperty(propertyName, value);
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

    public override void AppendException(Exception exception)
    {
        _state.ThrowIfDisposed();
        _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");
    }

    public override void AppendResult<T>(T value)
    {
        _state.ThrowIfDisposed();
        StringBuilder journal = _state.BeginJournalEntry().Append($"`{_operationName}` result: ");
        ValueFormatting.AppendValue(journal, value);
    }

    public override void Escalate(LogLevel level)
    {
        _state.ThrowIfDisposed();
        LogLevel previousLevel = _state.Escalate(level);
        if (level > previousLevel)
            BeginJournalEntry().Append($"`{_operationName}` escalated from {previousLevel} to {level}.");
    }

    protected override void DisposeCore()
    {
        _state.ThrowIfDisposed();
        _state.BeginJournalEntry().Append($"`{_operationName}` complete.");
    }
}
