using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="SynchronizedOperationLog"/> and <see cref="SynchronizedSubOperationLog"/>,
/// holding the members whose implementation is identical between the two - everything except
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>, which only the root
/// decorator has. Non-generic: since <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> are
/// deliberately unrelated interfaces, there's no type this base class could hold the wrapped <c>inner</c>
/// log as that would let it call <c>inner.AddProperty(...)</c>, <c>inner.Append(...)</c>, etc. directly - so
/// unlike <see cref="OperationLogBase"/> (whose shared members do their own work against a
/// concretely-typed <see cref="OperationLogState"/>), every member here that needs to reach the wrapped log
/// is split into a <see langword="void"/> (or, for
/// <see cref="BeginSubOperation(string)"/>/<see cref="Dispose"/>, non-<see langword="void"/> but
/// non-self-returning) method here that does the locking, plus an abstract <c>...Core</c> method - the only
/// thing <see cref="SynchronizedOperationLog"/>/<see cref="SynchronizedSubOperationLog"/> need to
/// implement - that makes the one call to their own strongly-typed <c>_inner</c> field. Each decorator's own
/// fluent interface member (<see cref="IOperationLog.AddProperty{T}"/> and its siblings) is then a one-line
/// wrapper that calls the corresponding member here and returns <see langword="this"/>, the same shape as
/// <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> wrap <see cref="OperationLogBase"/>.
/// <see cref="EventId"/>/<see cref="IsEnabled"/> are abstract here too (rather than each decorator
/// redeclaring an unrelated property of the same name), purely so this class has something to forward
/// <see cref="IOperationLogBase.EventId"/>/<see cref="IOperationLogBase.IsEnabled"/> to - they don't need
/// locking, so there's no <c>...Core</c> split for them.
/// <para>
/// Also implements <see cref="IOperationLogBase"/> explicitly, for the same reason
/// <see cref="OperationLogBase"/> does - see that interface's doc comment.
/// </para>
/// </summary>
internal abstract class SynchronizedOperationLogBase(object gate) : IOperationLogBase
{
    protected readonly object _gate = gate;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties
    {
        get
        {
            lock (_gate)
                return PropertiesCore();
        }
    }

    protected abstract IReadOnlyList<KeyValuePair<string, object?>> PropertiesCore();

    public abstract EventId EventId { get; }

    public abstract bool IsEnabled { get; }

    protected void AddPropertyLocked<T>(string name, T value)
    {
        lock (_gate)
            AddPropertyCore(name, value);
    }

    protected abstract void AddPropertyCore<T>(string name, T value);

    protected void AppendExceptionLocked(Exception exception)
    {
        lock (_gate)
            AppendExceptionCore(exception);
    }

    protected abstract void AppendExceptionCore(Exception exception);

    protected void AppendResultLocked<T>(T value)
    {
        lock (_gate)
            AppendResultCore(value);
    }

    protected abstract void AppendResultCore<T>(T value);

    protected void EscalateLocked(LogLevel level)
    {
        lock (_gate)
            EscalateCore(level);
    }

    protected abstract void EscalateCore(LogLevel level);

    protected void AppendLocked(string text)
    {
        lock (_gate)
            AppendCore(text);
    }

    protected abstract void AppendCore(string text);

    // The handler evaluated text into a private buffer, not the shared journal, precisely because it
    // can't hold `_gate` while doing so - see OperationLogInterpolatedStringHandler's doc comment. Splice
    // that buffer into the real journal here, under the lock, then return it to the pool. The
    // IJournalOwner check/splice is shareable without an abstract hook - IJournalOwner is an internal
    // interface unaffected by IOperationLog/ISubOperationLog being unrelated - so only the plain-string
    // fallback (the `else` branch) needs one.
    protected void AppendLocked(ref OperationLogInterpolatedStringHandler text)
    {
        if (!text.IsEnabled)
            return;

        StringBuilder rented = text.RentedBuilder!;
        try
        {
            lock (_gate)
            {
                if (InnerJournalOwnerCore() is IJournalOwner owner)
                {
                    StringBuilder journal = owner.BeginJournalEntry();
                    foreach (ReadOnlyMemory<char> chunk in rented.GetChunks())
                        journal.Append(chunk.Span);
                }
                else
                {
                    AppendCore(rented.ToString());
                }
            }
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }
    }

    protected abstract IJournalOwner? InnerJournalOwnerCore();

    protected void AppendValueLocked<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            AppendValueCore(value, valueName);
    }

    protected abstract void AppendValueCore<T>(T value, string? valueName);

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    protected void AppendJsonLocked<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            AppendJsonCore(value, valueName);
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    protected abstract void AppendJsonCore<T>(T value, string? valueName);

    public ISubOperationLog BeginSubOperation(string operationName)
    {
        ISubOperationLog subOperation;
        lock (_gate)
            subOperation = BeginSubOperationCore(operationName);
        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        ISubOperationLog subOperation;

        if (!operationName.IsEnabled)
        {
            // Nothing was evaluated - inner.BeginSubOperation(string) ignores its argument when inner
            // is itself disabled, so the name doesn't matter here.
            lock (_gate)
                subOperation = BeginSubOperationCore(string.Empty);
            return new SynchronizedSubOperationLog(subOperation, _gate);
        }

        StringBuilder rented = operationName.RentedBuilder!;
        try
        {
            string name = rented.ToString();
            lock (_gate)
                subOperation = BeginSubOperationCore(name);
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }

        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    protected abstract ISubOperationLog BeginSubOperationCore(string operationName);

    public void Dispose()
    {
        lock (_gate)
            DisposeCore();
    }

    protected abstract void DisposeCore();

    IReadOnlyList<KeyValuePair<string, object?>> IOperationLogBase.Properties => Properties;

    EventId IOperationLogBase.EventId => EventId;

    bool IOperationLogBase.IsEnabled => IsEnabled;

    void IOperationLogBase.Escalate(LogLevel level) => EscalateLocked(level);

    void IOperationLogBase.AddProperty<T>(string name, T value) => AddPropertyLocked(name, value);

    void IOperationLogBase.AppendException(Exception exception) => AppendExceptionLocked(exception);

    void IOperationLogBase.AppendResult<T>(T value) => AppendResultLocked(value);

    void IOperationLogBase.Append(string text) => AppendLocked(text);

    void IOperationLogBase.Append(ref OperationLogInterpolatedStringHandler text) => AppendLocked(ref text);

    void IOperationLogBase.AppendValue<T>(T value, string? valueName) => AppendValueLocked(value, valueName);

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    void IOperationLogBase.AppendJson<T>(T value, string? valueName) => AppendJsonLocked(value, valueName);

    ISubOperationLog IOperationLogBase.BeginSubOperation(string operationName) => BeginSubOperation(operationName);

    ISubOperationLog IOperationLogBase.BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName) => BeginSubOperation(ref operationName);
}
