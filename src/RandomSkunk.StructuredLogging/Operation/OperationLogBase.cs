using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="RootOperationLog"/> and <see cref="ChildOperationLog"/>, holding the members
/// whose implementations are identical between the two. Non-generic: since <see cref="IOperationLog"/> and
/// <see cref="ISubOperationLog"/> are deliberately unrelated interfaces (neither extends the other), a
/// member declared here can't return "whichever interface the derived class implements" the way it could if
/// both shared a common generic ancestor. Instead, this class implements <see cref="IOperationLogBase"/>
/// directly and non-explicitly, with plain public members (<see cref="AddProperty{T}"/>,
/// <see cref="Append(string)"/>, <see cref="Append(ref OperationLogInterpolatedStringHandler)"/>,
/// <see cref="AppendValue{T}"/>, <see cref="AppendJson{T}"/>, and - since the journal message each writes
/// differs between root and sub-operation - the abstract <see cref="AppendException"/>,
/// <see cref="AppendResult{T}"/>, <see cref="Escalate"/>) that return <see langword="void"/> rather than a
/// self-type. <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> each add a same-named,
/// self-returning explicit implementation of <see cref="IOperationLog"/>'s/<see cref="ISubOperationLog"/>'s
/// fluent counterpart - a one-line wrapper that calls the member here (or, for the three abstract ones,
/// provides it) and returns <see langword="this"/>; that explicit member doesn't collide with the one
/// declared here because it satisfies a different interface, with a different return type.
/// <see cref="BeginSubOperation(string)"/> and <see cref="Dispose"/> don't have this problem - both
/// interfaces declare them with the same, non-self return type (<see cref="ISubOperationLog"/> and
/// <see langword="void"/> respectively) - so they're fully implemented here and satisfy both interfaces'
/// members directly.
/// </summary>
internal abstract class OperationLogBase(OperationLogState state, string operationName) : IJournalOwner, IOperationLogBase
{
    protected readonly OperationLogState _state = state;
    protected readonly string _operationName = operationName;

    private int _disposed;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        _state.Properties ?? (IReadOnlyList<KeyValuePair<string, object?>>)[];

    public EventId EventId => _state.EventId;

    public bool IsEnabled => true;

    /// <summary>
    /// Throws if the root operation has been disposed, then appends the "[elapsed] " timestamp prefix
    /// that starts every journal line - see <see cref="IJournalOwner"/>.
    /// </summary>
    public StringBuilder BeginJournalEntry()
    {
        _state.ThrowIfDisposed();
        return _state.BeginJournalEntry();
    }

    public void AddProperty<T>(string propertyName, T value)
    {
        _state.ThrowIfDisposed();

        // Validated here rather than left to fail on its own: a null name passes through
        // OperationLogState.AddProperty untouched and only throws later, from FinalizeHeader's
        // journal insert - which runs inside Dispose, so the NullReferenceException surfaces far
        // from the offending call, takes the entire log entry with it, and masks whatever
        // exception was already in flight through the surrounding `using`.
        ArgumentNullException.ThrowIfNull(propertyName);

        // The reserved names are passed to Write as trailing tuple arguments, never through this
        // same list, so a caller using one here can't actually overwrite them; it only means the
        // sink sees the key twice. That's the same thing Microsoft.Extensions.Logging itself does
        // nothing about for two ordinary duplicate template holes - resolving duplicate keys is
        // sink policy, not something this library enforces - so this warns rather than throws,
        // just making the mistake visible in the one place a developer will actually look instead
        // of silently leaving it to the sink.
        if (IsReservedProperty(propertyName))
        {
            BeginJournalEntry().Append('"').Append(propertyName)
                .Append("\" is a reserved property name; the operation's own property of that name will be duplicated in the log entry.");
        }

        _state.AddProperty(propertyName, value);
    }

    public void Append(string text) => BeginJournalEntry().Append(text);

    public void Append(ref OperationLogInterpolatedStringHandler text)
    {
        // The handler already wrote everything directly into the journal (via BeginJournalEntry, in
        // its constructor) while it was being built - nothing left to do here.
    }

    public void AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendValue(journal, value);
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public void AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendJson(journal, value);
    }

    // Not shared between RootOperationLog/ChildOperationLog - the journal message each writes differs
    // ("Operation failed: ..."/"Operation escalated..." on the root vs "`name` failed: ..."/"`name`
    // escalated..." on a sub-operation) - but declared here anyway (rather than directly on
    // IOperationLog.AppendException/ISubOperationLog.AppendException) so this class has a single place
    // that explicitly implements all of IOperationLogBase.
    public abstract void AppendException(Exception exception);

    public abstract void AppendResult<T>(T value);

    public abstract void Escalate(LogLevel level);

    public ISubOperationLog BeginSubOperation(string subOperationName)
    {
        BeginJournalEntry().Append($"`{subOperationName}` started.");
        return new ChildOperationLog(_state, subOperationName);
    }

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        if (operationName.DirectTarget is not StringBuilder journal)
        {
            // Can't happen for RootOperationLog/ChildOperationLog - both are IJournalOwner, so the
            // handler always writes directly into the real journal - but fall back safely rather than
            // assume it, in case that ever changes.
            string fallbackName = operationName.RentedBuilder?.ToString() ?? string.Empty;
            if (operationName.RentedBuilder is StringBuilder rented)
                OperationLogPools.Journals.Return(rented);
            return BeginSubOperation(fallbackName);
        }

        int start = operationName.DirectStartIndex;
        string subOperationName = journal.ToString(start, journal.Length - start);
        journal.Insert(start, '`').Append("` started.");
        return new ChildOperationLog(_state, subOperationName);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, -1) == -1)
            return;

        DisposeCore();
    }

    protected abstract void DisposeCore();

    private static bool IsReservedProperty(string propertyName)
    {
        if (propertyName.Length >= 14)
        {
            ReadOnlySpan<char> span = propertyName;
            if (span.StartsWith("Operation."))
            {
                span = span[10..];
                if (span.SequenceEqual("Name") || span.SequenceEqual("Result") || span.SequenceEqual("StartTime") || span.SequenceEqual("DurationSeconds"))
                    return true;
            }
        }

        return false;
    }
}
