using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A do-nothing test double implementing both <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/>
/// on a single type. Hand an instance - or a mock of this type - to code that takes either interface.
/// Nothing is recorded beyond <c>Properties</c> and nothing is written to any logger; every fluent
/// member just returns <see langword="this"/> (as whichever interface the call arrived through), so a
/// chain of calls behaves the way a real operation's would.
/// <para>
/// <b>Why this type exists.</b> Mocking <see cref="IOperationLog"/> or <see cref="ISubOperationLog"/>
/// directly doesn't work: <see cref="IOperationLogBase{TSelf}.Append(ref OperationLogInterpolatedStringHandler)"/>
/// and <see cref="IOperationLogBase{TSelf}.BeginSubOperation(ref OperationLogInterpolatedStringHandler)"/>
/// take a <see langword="ref"/> <see langword="struct"/> parameter, which a proxy-generating framework
/// (Moq, NSubstitute, and anything else built on Castle DynamicProxy) can't forward - the proxy it
/// generates for those two members is invalid, and invoking either one throws
/// <see cref="InvalidProgramException"/> at run time. Since <c>log.Append($"...")</c> is the idiomatic
/// call, a plain mock of either interface fails on contact with the code it's meant to test.
/// </para>
/// <para>
/// <b>Why one type instead of two.</b> <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> are
/// deliberately unrelated interfaces, each extending the generic <see cref="IOperationLogBase{TSelf}"/>
/// closed over itself, so most of their fluent members share a name and parameter list but differ only in
/// declared return type. A single class can implement both by giving each shared member name one
/// <i>public, non-interface</i> virtual method - always returning <see langword="void"/>, since a method's
/// return type can't vary by which interface the caller used - plus two explicit interface
/// implementations (one per closed interface) that call the virtual method and return
/// <see langword="this"/> as whichever interface they implement. The <see langword="void"/> return isn't
/// just a workaround for the return-type clash, either: it's also what makes the fake work without
/// Moq's <c>CallBase</c>. If the mockable member itself returned <see langword="this"/>'s interface type,
/// an unconfigured mock (no <c>CallBase</c>, no setup) would return <see langword="null"/> for it - Moq's
/// default for an unconfigured reference-typed member - and the very next call in the chain would throw a
/// <see cref="NullReferenceException"/>. Because the virtual member returns <see langword="void"/> instead,
/// an unconfigured mock simply does nothing when it's invoked; the actual `return this` lives only in the
/// non-virtual (from a proxy's perspective) explicit interface implementation, which always runs for real
/// and can't be intercepted, so the fluent chain keeps working with no <c>CallBase</c> and no setup at all.
/// Set up or verify the virtual member; the explicit implementations exist only as plumbing, and (being
/// explicit) can't be reached unqualified even from inside this class, so they can't accidentally recurse
/// into each other. Members whose return type doesn't vary between the two interfaces
/// (<c>Properties</c>, <see cref="EventId"/>, <see cref="Dispose()"/>) don't strictly need a split for
/// this reason. <see cref="EventId"/> keeps a single ordinary member. <see cref="Dispose()"/> gets a split
/// anyway, but for an unrelated reason - see its own remarks below. <c>Properties</c> and <c>IsEnabled</c>
/// instead have no public counterpart at all: both are implemented purely as a pair of explicit
/// implementations, one per closed interface, each returning the value directly - there's nothing for a
/// test to override, so neither is virtual either.
/// </para>
/// <para>
/// <b>Properties and EventId are genuinely shared.</b> Because a "sub-operation" handed out by
/// <see cref="BeginSubOperation(string)"/> is this same instance, <c>Properties</c> and
/// <see cref="EventId"/> behave exactly as they do for a real operation: a property added through a
/// sub-operation reference lands in the same list a caller sees through the original
/// <see cref="IOperationLog"/> reference.
/// </para>
/// <para>
/// <b>Dispose is also shared - unlike a real operation.</b> A real <c>ChildOperationLog</c>'s
/// <see cref="IDisposable.Dispose"/> only marks that sub-operation complete; only the root's
/// <see cref="IDisposable.Dispose"/> flushes the eventual log entry. Here, since
/// <see cref="BeginSubOperation(string)"/> returns <see langword="this"/>, disposing a "sub-operation" and
/// disposing the root invoke the exact same <see cref="Dispose()"/> override - test code that disposes
/// both must expect <see cref="Dispose()"/> to be called once per <see langword="using"/> block, not once
/// per operation.
/// </para>
/// <para>
/// <b><c>IsEnabled</c> is always <see langword="true"/>.</b> With no public counterpart to override or
/// mock (see above), it can never be made to return <see langword="false"/> - this fake has no disabled
/// state, so its interpolated <c>Append</c>/<c>BeginSubOperation</c> overloads always evaluate their
/// holes. To test code against a genuinely disabled operation, begin one on
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance"/> instead (see the README's
/// "Testing code that takes an <c>IOperationLog</c>" section) rather than trying to make this fake report
/// itself as disabled.
/// </para>
/// </summary>
public class FakeOperationLog : IOperationLog, ISubOperationLog, IJournalOwner
{
    private readonly StringBuilder _journal = new();
    private readonly List<KeyValuePair<string, object?>> _properties = [];

    /// <summary>
    /// The properties recorded by <see cref="AddProperty{T}"/>, shared between however many times this
    /// instance was handed out as the root <see cref="IOperationLog"/> or as a "sub-operation"
    /// <see cref="ISubOperationLog"/> - see the remarks on <see cref="FakeOperationLog"/>.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, object?>> IOperationLogBase<IOperationLog>.Properties => _properties;

    /// <inheritdoc/>
    IReadOnlyList<KeyValuePair<string, object?>> IOperationLogBase<ISubOperationLog>.Properties => _properties;

    /// <summary>
    /// Always <see langword="default"/>. Override this (or set it up on a mock) to return a specific
    /// <see cref="EventId"/>.
    /// </summary>
    public virtual EventId EventId => default;

    /// <inheritdoc/>
    bool IOperationLogBase<IOperationLog>.IsEnabled => true;

    /// <inheritdoc/>
    bool IOperationLogBase<ISubOperationLog>.IsEnabled => true;

    /// <summary>
    /// Hands the interpolated string handler this instance's own buffer to append into, undecorated - no
    /// timestamp, no newline - so that <see cref="Append(string)"/> and
    /// <see cref="BeginSubOperation(string)"/> receive exactly the text the caller interpolated.
    /// </summary>
    StringBuilder IJournalOwner.BeginJournalEntry() => _journal;

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.Escalate"/> implementations -
    /// deliberately non-virtual, see <see cref="FakeOperationLog"/> - so a test can set this up or verify
    /// it in place of either.
    /// </summary>
    /// <param name="level">Ignored.</param>
    public virtual void Escalate(LogLevel level)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.AddProperty<T>(string name, T value)
    {
        _properties.Add(new KeyValuePair<string, object?>(name, value));
        AddProperty(name, value);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AddProperty<T>(string name, T value)
    {
        _properties.Add(new KeyValuePair<string, object?>(name, value));
        AddProperty(name, value);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.AddProperty{T}"/> implementations
    /// after the property has already been recorded in <c>Properties</c>, so a test can set this up
    /// or verify it in place of either - overriding it does not prevent the property from being recorded.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    public virtual void AddProperty<T>(string name, T value)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.AppendException"/> implementations,
    /// so a test can set this up or verify it in place of either.
    /// </summary>
    /// <param name="exception">Ignored.</param>
    public virtual void AppendException(Exception exception)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.AppendResult{T}"/> implementations,
    /// so a test can set this up or verify it in place of either.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">Ignored.</param>
    public virtual void AppendResult<T>(T value)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.SetException(Exception exception)
    {
        SetException(exception);
        return this;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <param name="exception">Ignored.</param>
    public virtual void SetException(Exception exception)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.SetResult<T>(T value)
    {
        SetResult(value);
        return this;
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="value">Ignored.</param>
    public virtual void SetResult<T>(T value)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.Append(string text)
    {
        Append(text);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.Append(string text)
    {
        Append(text);
        return this;
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.Append([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler text) =>
        ((IOperationLogBase<IOperationLog>)this).Append(TakeText());

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.Append(ref OperationLogInterpolatedStringHandler text) =>
        ((IOperationLogBase<ISubOperationLog>)this).Append(TakeText());

    /// <summary>
    /// Does nothing. Called by every <c>Append</c> overload - a call using an interpolated string handler
    /// arrives here with the interpolated text already built, so a test can set this up or verify it in
    /// place of any of them.
    /// </summary>
    /// <param name="text">The text the caller wrote (already interpolated, if that overload was used).</param>
    public virtual void Append(string text)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLogBase<IOperationLog>.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.AppendValue{T}"/> implementations,
    /// so a test can set this up or verify it in place of either.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">Ignored.</param>
    /// <param name="valueName">Ignored.</param>
    public virtual void AppendValue<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    IOperationLog IOperationLogBase<IOperationLog>.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    ISubOperationLog IOperationLogBase<ISubOperationLog>.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLogBase{TSelf}.AppendJson{T}"/> implementations,
    /// so a test can set this up or verify it in place of either.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">Ignored.</param>
    /// <param name="valueName">Ignored.</param>
    /// <remarks>
    /// This override serializes nothing, but carries the same trimming/Native AOT annotations as the
    /// members it implements, which the trim analyzer requires to match.
    /// </remarks>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    public virtual void AppendJson<T>(T value, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<IOperationLog>.BeginSubOperation(string operationName)
    {
        BeginSubOperation(operationName);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.BeginSubOperation(string operationName)
    {
        BeginSubOperation(operationName);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<IOperationLog>.BeginSubOperation([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler operationName) =>
        ((IOperationLogBase<IOperationLog>)this).BeginSubOperation(TakeText());

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase<ISubOperationLog>.BeginSubOperation(ref OperationLogInterpolatedStringHandler operationName) =>
        ((IOperationLogBase<ISubOperationLog>)this).BeginSubOperation(TakeText());

    /// <summary>
    /// Does nothing. Called by every <c>BeginSubOperation</c> overload, which then returns
    /// <see langword="this"/> - a "sub-operation" of this fake is this same instance, cast to
    /// <see cref="ISubOperationLog"/> - see the remarks on <see cref="FakeOperationLog"/>.
    /// </summary>
    /// <param name="operationName">The name the caller wrote (already interpolated, if that overload was used).</param>
    public virtual void BeginSubOperation(string operationName)
    {
    }

    /// <summary>
    /// Calls <see cref="GC.SuppressFinalize(object)"/> - this type has no finalizer, but CA1816 still flags a
    /// <see cref="IDisposable.Dispose"/> that doesn't call it - then <see cref="Dispose()"/>. Explicit
    /// rather than folded into <see cref="Dispose()"/> itself so the no-op body a test overrides stays
    /// exactly that: a no-op, with no analyzer-driven boilerplate for a subclass to accidentally omit or
    /// duplicate.
    /// </summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose();
    }

    /// <summary>
    /// Does nothing. Virtual so a test can verify that the code under test disposed the operation -
    /// forgetting to is the defect RSSL0006 exists to catch. Called by the single explicit
    /// <see cref="IDisposable.Dispose"/> above, which satisfies <see cref="IOperationLog"/>'s and
    /// <see cref="ISubOperationLog"/>'s <see cref="IDisposable"/> at once (both declare a plain
    /// <see langword="void"/> <c>Dispose()</c>, so - unlike the other fluent members - no per-interface
    /// split is needed). Because <see cref="BeginSubOperation(string)"/> hands back this same instance,
    /// disposing a "sub-operation" invokes this exact override too - see the remarks on
    /// <see cref="FakeOperationLog"/>.
    /// </summary>
    public virtual void Dispose()
    {
    }

    private string TakeText()
    {
        string text = _journal.ToString();
        _journal.Clear();
        return text;
    }
}
