using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="RootOperationLog"/> and <see cref="ChildOperationLog"/>, holding the members
/// whose implementations are identical between the two. Non-generic: since <see cref="IOperationLog"/> and
/// <see cref="ISubOperationLog"/> are deliberately unrelated interfaces (neither extends the other, and
/// neither extends a shared base), a member declared here can't return "whichever interface the derived
/// class implements" the way it could when both interfaces shared a common generic ancestor. Instead, every
/// member that would otherwise need to return that self-type (<see cref="AddPropertyCore{T}"/>,
/// <see cref="AppendCore(string)"/>, <see cref="AppendCore(ref OperationLogInterpolatedStringHandler)"/>,
/// <see cref="AppendValueCore{T}"/>, <see cref="AppendJsonCore{T}"/>, and - since the journal message each
/// writes differs between root and sub-operation - the abstract <see cref="AppendExceptionCore"/>,
/// <see cref="AppendResultCore{T}"/>, <see cref="EscalateCore"/>) is declared here, suffixed <c>Core</c> (to
/// avoid a same-signature-different-return-type member hiding the interface-satisfying one declared on
/// <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/>), returning <see langword="void"/>;
/// <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> each declare the real,
/// interface-satisfying fluent member as a one-line wrapper that calls the <see langword="void"/> version
/// here (or, for the three abstract ones, provides it) and returns <see langword="this"/>.
/// <see cref="BeginSubOperation(string)"/> and <see cref="Dispose"/> don't have this problem - both
/// interfaces declare them with the same, non-self return type (<see cref="ISubOperationLog"/> and
/// <see langword="void"/> respectively) - so they're fully implemented here and satisfy both interfaces'
/// members implicitly, exactly as before.
/// <para>
/// Also implements <see cref="IOperationLogBase"/> explicitly, purely so the <c>Core</c> members above have
/// a declared interface contract - <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> never
/// consume it, only <see cref="OperationLogBase"/> itself.
/// </para>
/// </summary>
internal abstract class OperationLogBase(OperationLogState state, string operationName) : IJournalOwner, IOperationLogBase
{
    protected readonly OperationLogState _state = state;
    protected readonly string _operationName = operationName;

    private int _disposed;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties =>
        _state.Properties ?? (IReadOnlyList<KeyValuePair<string, object?>>)[];

    public EventId EventId => _state.EventId;

#pragma warning disable CA1822 // Mark members as static
    public bool IsEnabled => true;
#pragma warning restore CA1822 // Mark members as static

    protected void AddPropertyCore<T>(string name, T value)
    {
        _state.ThrowIfDisposed();

        // Validated here rather than left to fail on its own: a null name passes through
        // OperationLogState.AddProperty untouched and only throws later, from FinalizeHeader's
        // journal insert - which runs inside Dispose, so the NullReferenceException surfaces far
        // from the offending call, takes the entire log entry with it, and masks whatever
        // exception was already in flight through the surrounding `using`.
        ArgumentNullException.ThrowIfNull(name);

        _state.AddProperty(name, value);
    }

    /// <summary>
    /// Throws if the root operation has been disposed, then appends the "[elapsed] " timestamp prefix
    /// that starts every journal line - see <see cref="IJournalOwner"/>.
    /// </summary>
    public StringBuilder BeginJournalEntry()
    {
        _state.ThrowIfDisposed();
        return _state.BeginJournalEntry();
    }

    protected void AppendCore(string text) => BeginJournalEntry().Append(text);

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
    protected void AppendCore(ref OperationLogInterpolatedStringHandler text)
    {
        // The handler already wrote everything directly into the journal (via BeginJournalEntry, in
        // its constructor) while it was being built - nothing left to do here.
    }
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter

    protected void AppendValueCore<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendValue(journal, value);
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    protected void AppendJsonCore<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        StringBuilder journal = BeginJournalEntry().Append($"`{valueName}`: ");
        ValueFormatting.AppendJson(journal, value);
    }

    // Not shared between RootOperationLog/ChildOperationLog - the journal message each writes differs
    // ("Operation failed: ..."/"Operation escalated..." on the root vs "`name` failed: ..."/"`name`
    // escalated..." on a sub-operation) - but declared here anyway (rather than directly on
    // IOperationLog.AppendException/ISubOperationLog.AppendException) so this class has a single place
    // that explicitly implements all of IOperationLogBase.
    protected abstract void AppendExceptionCore(Exception exception);

    protected abstract void AppendResultCore<T>(T value);

    protected abstract void EscalateCore(LogLevel level);

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

    void IOperationLogBase.Escalate(LogLevel level) => EscalateCore(level);

    void IOperationLogBase.AddProperty<T>(string name, T value) => AddPropertyCore(name, value);

    void IOperationLogBase.AppendException(Exception exception) => AppendExceptionCore(exception);

    void IOperationLogBase.AppendResult<T>(T value) => AppendResultCore(value);

    void IOperationLogBase.Append(string text) => AppendCore(text);

    void IOperationLogBase.Append(ref OperationLogInterpolatedStringHandler text) => AppendCore(ref text);

    void IOperationLogBase.AppendValue<T>(T value, string? valueName) => AppendValueCore(value, valueName);

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    void IOperationLogBase.AppendJson<T>(T value, string? valueName) => AppendJsonCore(value, valueName);
}
