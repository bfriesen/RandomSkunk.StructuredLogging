using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A nested sub-operation returned by <see cref="IOperationLog.BeginSubOperation(string)"/> (on either the
/// root operation or another sub-operation). Never writes its own log entry - every member only ever appends
/// to the shared <see cref="OperationLogState._journal"/>. Applies no synchronization of its own - see
/// <see cref="SynchronizedSubOperationLog"/> for the decorator that wraps this type when an operation is
/// begun with <c>threadSafe: true</c>. Every fluent member shared with <see cref="RootOperationLog"/> is a
/// <c>...Core</c>-suffixed <see langword="void"/> method on <see cref="OperationLogBase"/>; this class's own
/// same-named members are the ones that actually satisfy <see cref="ISubOperationLog"/>, each just calling
/// the base version and returning <see langword="this"/>.
/// </summary>
internal sealed class ChildOperationLog(OperationLogState state, string operationName)
    : OperationLogBase(state, operationName), ISubOperationLog
{
    public ISubOperationLog AddProperty<T>(string propertyName, T value)
    {
        base.AddPropertyCore(propertyName, value);
        return this;
    }

    public ISubOperationLog Append(string text)
    {
        base.AppendCore(text);
        return this;
    }

    public ISubOperationLog Append(ref OperationLogInterpolatedStringHandler text)
    {
        base.AppendCore(ref text);
        return this;
    }

    public ISubOperationLog AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendValueCore(value, valueName);
        return this;
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public ISubOperationLog AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        base.AppendJsonCore(value, valueName);
        return this;
    }

    public ISubOperationLog AppendException(Exception exception)
    {
        AppendExceptionCore(exception);
        return this;
    }

    public ISubOperationLog AppendResult<T>(T value)
    {
        AppendResultCore(value);
        return this;
    }

    public ISubOperationLog Escalate(LogLevel level)
    {
        EscalateCore(level);
        return this;
    }

    protected override void AppendExceptionCore(Exception exception)
    {
        _state.ThrowIfDisposed();
        _state.BeginJournalEntry().Append($"`{_operationName}` failed:\n{exception}");
    }

    protected override void AppendResultCore<T>(T value)
    {
        _state.ThrowIfDisposed();
        StringBuilder journal = _state.BeginJournalEntry().Append($"`{_operationName}` result: ");
        ValueFormatting.AppendValue(journal, value);
    }

    protected override void EscalateCore(LogLevel level)
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
