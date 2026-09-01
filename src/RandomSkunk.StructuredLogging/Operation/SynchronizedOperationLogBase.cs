using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Base class for <see cref="SynchronizedOperationLog"/> and <see cref="SynchronizedSubOperationLog"/>,
/// holding the members whose implementation is identical between the two - everything except
/// <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>, which only the root
/// decorator has. Holds the wrapped log as <see cref="IOperationLogBase"/> - since <see cref="IOperationLog"/>
/// and <see cref="ISubOperationLog"/> both extend it, this base class can call <c>_inner.AddProperty(...)</c>,
/// <c>_inner.Append(...)</c>, etc. directly under <c>_gate</c>, the same way <see cref="OperationLogBase"/>'s
/// shared members do their own work against a concretely-typed <see cref="OperationLogState"/>, without
/// needing an abstract method per member for a decorator to supply its own strongly-typed forwarding call.
/// Implements <see cref="IOperationLogBase"/> directly and non-explicitly, so its own public members already
/// satisfy that interface's <see langword="void"/>-returning contract. Each decorator's own fluent interface
/// member (<see cref="IOperationLog.AddProperty{T}"/> and its siblings) is then a one-line, explicit
/// implementation that calls the corresponding member here and returns <see langword="this"/>, the same
/// shape as <see cref="RootOperationLog"/>/<see cref="ChildOperationLog"/> wrap <see cref="OperationLogBase"/>.
/// <see cref="SynchronizedOperationLog"/> additionally keeps its own <see cref="IOperationLog"/>-typed
/// <c>_inner</c> field, since <see cref="IOperationLog.SetException"/>/<see cref="IOperationLog.SetResult{T}"/>
/// aren't declared on <see cref="IOperationLogBase"/> and so this base class has no way to reach them.
/// </summary>
internal abstract class SynchronizedOperationLogBase(IOperationLogBase inner, object gate) : IOperationLogBase
{
    private readonly IOperationLogBase _inner = inner;

    protected readonly object _gate = gate;

    public IReadOnlyList<KeyValuePair<string, object?>> Properties
    {
        get
        {
            lock (_gate)
                return [.. _inner.Properties];
        }
    }

    public EventId EventId => _inner.EventId;

    public bool IsEnabled => _inner.IsEnabled;

    public void AddProperty<T>(string propertyName, T value)
    {
        lock (_gate)
            _inner.AddProperty(propertyName, value);
    }

    public void AppendException(Exception exception)
    {
        lock (_gate)
            _inner.AppendException(exception);
    }

    public void AppendResult<T>(T value)
    {
        lock (_gate)
            _inner.AppendResult(value);
    }

    public void Escalate(LogLevel level)
    {
        lock (_gate)
            _inner.Escalate(level);
    }

    public void Append(string text)
    {
        lock (_gate)
            _inner.Append(text);
    }

    // The handler evaluated text into a private buffer, not the shared journal, precisely because it
    // can't hold `_gate` while doing so - see OperationLogInterpolatedStringHandler's doc comment. Splice
    // that buffer into the real journal here, under the lock, then return it to the pool. The
    // IJournalOwner check/splice is shareable without an abstract hook - IJournalOwner is an internal
    // interface unaffected by IOperationLog/ISubOperationLog being unrelated - so only the plain-string
    // fallback (the `else` branch) needs one.
    public void Append(ref OperationLogInterpolatedStringHandler text)
    {
        if (!text.IsEnabled)
            return;

        StringBuilder rented = text.RentedBuilder!;
        try
        {
            lock (_gate)
            {
                if (_inner is IJournalOwner owner)
                {
                    StringBuilder journal = owner.BeginJournalEntry();
                    foreach (ReadOnlyMemory<char> chunk in rented.GetChunks())
                        journal.Append(chunk.Span);
                }
                else
                {
                    _inner.Append(rented.ToString());
                }
            }
        }
        finally
        {
            OperationLogPools.Journals.Return(rented);
        }
    }

    public void AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            _inner.AppendValue(value, valueName);
    }

    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public void AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        lock (_gate)
            _inner.AppendJson(value, valueName);
    }

    public ISubOperationLog BeginSubOperation(string operationName)
    {
        ISubOperationLog subOperation;
        lock (_gate)
            subOperation = _inner.BeginSubOperation(operationName);
        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    public ISubOperationLog BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName)
    {
        ISubOperationLog subOperation;

        if (operationName.IsEnabled)
        {
            StringBuilder rented = operationName.RentedBuilder!;
            try
            {
                string name = rented.ToString();
                lock (_gate)
                    subOperation = _inner.BeginSubOperation(name);
            }
            finally
            {
                OperationLogPools.Journals.Return(rented);
            }
        }
        else
        {
            // Nothing was evaluated - inner.BeginSubOperation(string) ignores its argument when inner
            // is itself disabled, so the name doesn't matter here.
            lock (_gate)
                subOperation = _inner.BeginSubOperation(string.Empty);
        }

        return new SynchronizedSubOperationLog(subOperation, _gate);
    }

    public void Dispose()
    {
        lock (_gate)
            _inner.Dispose();
    }
}
