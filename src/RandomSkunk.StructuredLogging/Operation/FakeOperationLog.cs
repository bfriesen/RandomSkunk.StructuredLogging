using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// A do-nothing test double implementing both <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/>
/// on a single type. Hand a subclass instance - or a mock of one - to code that takes either interface.
/// Nothing is recorded beyond <c>Properties</c> and nothing is written to any logger; every fluent member
/// but <see cref="BeginSubOperation(string)"/> just returns <see langword="this"/> (as whichever interface
/// the call arrived through), so a chain of calls behaves the way a real operation's would.
/// <see cref="BeginSubOperation(string)"/> itself is abstract - see its own remarks.
/// <para>
/// <b>Why this type exists.</b> Mocking <see cref="IOperationLog"/> or <see cref="ISubOperationLog"/>
/// directly doesn't work: <see cref="IOperationLog.Append(ref OperationLogInterpolatedStringHandler)"/>
/// and <see cref="IOperationLogBase.BeginSubOperation(ref OperationLogInterpolatedStringHandler)"/> - and
/// their <see cref="ISubOperationLog"/> equivalents - take a <see langword="ref"/> <see langword="struct"/>
/// parameter, which a proxy-generating framework
/// (Moq, NSubstitute, and anything else built on Castle DynamicProxy) can't forward - the proxy it
/// generates for those two members is invalid, and invoking either one throws
/// <see cref="InvalidProgramException"/> at run time. Since <c>log.Append($"...")</c> is the idiomatic
/// call, a plain mock of either interface fails on contact with the code it's meant to test.
/// </para>
/// <para>
/// <b>Why one type instead of two.</b> <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/> are
/// unrelated to each other (neither extends the other), but both extend the shared
/// <see cref="IOperationLogBase"/>. Only the fluent members whose return type has to be the self-interface -
/// <see cref="Escalate"/>, <see cref="AddProperty{T}"/>, <see cref="AppendException"/>,
/// <see cref="AppendResult{T}"/>, <c>Append</c>, <see cref="AppendValue{T}"/>, and <see cref="AppendJson{T}"/> -
/// are redeclared (with <see langword="new"/>) on each of <see cref="IOperationLog"/>/
/// <see cref="ISubOperationLog"/>, so most of them share a name and parameter list but differ only in
/// declared return type. A single class can implement both, for each such member name, by giving it one
/// <i>public, non-interface</i> virtual method - always returning <see langword="void"/>, since a method's
/// return type can't vary by which interface the caller used - plus one explicit interface implementation
/// per redeclaring interface (normally two: <see cref="IOperationLog"/> and <see cref="ISubOperationLog"/>)
/// that calls the virtual method and returns <see langword="this"/> as whichever interface it implements.
/// <see cref="IOperationLogBase"/>'s own (<see langword="void"/>-returning) copy of the same member is
/// usually satisfied implicitly by that same public virtual method - both are <see langword="void"/>, so no
/// third implementation is needed - except where the two wrappers do something beyond just calling it, which
/// happens twice: <see cref="AddProperty{T}"/>'s wrappers also record into <c>Properties</c> before calling
/// the virtual hook, and the interpolated <c>Append</c> overload's wrappers have no virtual method of their
/// own signature to fall back on (they call the plain-<see langword="string"/> virtual hook instead). Both
/// get a third, <see cref="IOperationLogBase"/>-qualified explicit implementation with the same body, so
/// that view behaves identically to the other two. The <see langword="void"/> return isn't
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
/// into each other. Members whose return type is the same across <see cref="IOperationLog"/>,
/// <see cref="ISubOperationLog"/>, and <see cref="IOperationLogBase"/> - because they're inherited from
/// <see cref="IOperationLogBase"/> unchanged rather than redeclared - don't need any of this splitting:
/// <see cref="EventId"/> keeps a single ordinary member; <see cref="Dispose()"/> gets a split anyway, but for
/// an unrelated reason - see its own remarks below; <c>Properties</c> and <c>IsEnabled</c> instead have no
/// public counterpart at all, each implemented purely as a single <see cref="IOperationLogBase"/>-qualified
/// explicit implementation returning the value directly - there's nothing for a test to override, so
/// neither is virtual either. <see cref="BeginSubOperation(string)"/> doesn't follow any of this - see its
/// own remarks.
/// </para>
/// <para>
/// <b>Properties and EventId are shared exactly when a subclass wants them to be.</b> Nothing here forces
/// a "sub-operation" returned by <see cref="BeginSubOperation(string)"/> to be this same instance, unlike
/// the old default. Returning <see langword="this"/> is the simplest way to make <c>Properties</c> and
/// <see cref="EventId"/> behave exactly as they do for a real operation - a property added through either
/// reference lands in the one list both see - but the <see cref="FakeOperationLog(FakeOperationLog)"/>
/// constructor gets the same <c>Properties</c> sharing for a genuinely separate instance: it copies the
/// parent's <c>Properties</c> list by reference, so later additions through either instance are still
/// visible through both - see the next paragraph for why that split is often the better choice.
/// </para>
/// <para>
/// <b>Dispose is shared exactly when <see cref="BeginSubOperation(string)"/> returns <see langword="this"/>.</b>
/// A real <c>ChildOperationLog</c>'s <see cref="IDisposable.Dispose"/> only marks that sub-operation
/// complete; only the root's <see cref="IDisposable.Dispose"/> flushes the eventual log entry. Here, if
/// <see cref="BeginSubOperation(string)"/> returns <see langword="this"/>, disposing a "sub-operation" and
/// disposing the root invoke the exact same <see cref="Dispose()"/> override - test code that disposes
/// both must expect <see cref="Dispose()"/> to be called once per <see langword="using"/> block, not once
/// per operation. A subclass that returns a different <see cref="ISubOperationLog"/> from
/// <see cref="BeginSubOperation(string)"/> doesn't have this quirk - in particular, a new instance built
/// with the <see cref="FakeOperationLog(FakeOperationLog)"/> constructor gets its own independent
/// <see cref="Dispose()"/> call while still sharing <c>Properties</c> with the instance it was built from.
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
public abstract class FakeOperationLog : IOperationLog, ISubOperationLog, IJournalOwner
{
    private readonly StringBuilder _journal = new();
    private readonly List<KeyValuePair<string, object?>> _properties = [];

    /// <summary>
    /// Initializes a new instance with its own, empty <c>Properties</c> list - the shape a root operation
    /// (or a sub-operation that doesn't need to share <c>Properties</c> with anything) needs.
    /// </summary>
    protected FakeOperationLog()
    {
    }

    /// <summary>
    /// Initializes a new instance that shares <paramref name="parent"/>'s <c>Properties</c> list by
    /// reference, instead of starting with an empty one of its own - so a property added through either
    /// instance afterward is visible through both, matching how a real operation and its sub-operations
    /// share one <c>Properties</c> list. Call this from an override of
    /// <see cref="BeginSubOperation(string)"/> that returns a new instance instead of
    /// <see langword="this"/>, passing the instance <see cref="BeginSubOperation(string)"/> was called on -
    /// despite the parameter's name, that doesn't have to be the root itself; a sub-operation handing out
    /// its own nested sub-operation passes itself just the same way. Nothing else is shared: the new
    /// instance gets its own journal buffer, so it doesn't interfere with <paramref name="parent"/>'s
    /// in-flight interpolated text, and its own <see cref="Dispose()"/> - see the remarks on
    /// <see cref="FakeOperationLog"/>.
    /// </summary>
    /// <param name="parent">The operation log to share <c>Properties</c> with.</param>
    protected FakeOperationLog(FakeOperationLog parent)
    {
        _properties = parent._properties;
    }

    /// <summary>
    /// The properties recorded by <see cref="AddProperty{T}"/> - shared with a "sub-operation" when
    /// <see cref="BeginSubOperation(string)"/> either hands back this same instance or a new one built
    /// with the <see cref="FakeOperationLog(FakeOperationLog)"/> constructor, neither of which is
    /// guaranteed (see their own remarks) - see the remarks on <see cref="FakeOperationLog"/>.
    /// </summary>
    IReadOnlyList<KeyValuePair<string, object?>> IOperationLogBase.Properties => _properties;

    /// <summary>
    /// Always <see langword="default"/>. Override this (or set it up on a mock) to return a specific
    /// <see cref="EventId"/>.
    /// </summary>
    public virtual EventId EventId => default;

    /// <inheritdoc/>
    bool IOperationLogBase.IsEnabled => true;

    /// <summary>
    /// Hands the interpolated string handler this instance's own buffer to append into, undecorated - no
    /// timestamp, no newline - so that <see cref="Append(string)"/> and
    /// <see cref="BeginSubOperation(string)"/> receive exactly the text the caller interpolated.
    /// </summary>
    StringBuilder IJournalOwner.BeginJournalEntry() => _journal;

    /// <inheritdoc/>
    IOperationLog IOperationLog.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.Escalate(LogLevel level)
    {
        Escalate(level);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both <see cref="IOperationLog.Escalate"/>/<see cref="ISubOperationLog.Escalate"/>
    /// implementations - deliberately non-virtual, see <see cref="FakeOperationLog"/> - so a test can set
    /// this up or verify it in place of either.
    /// </summary>
    /// <param name="level">Ignored.</param>
    public virtual void Escalate(LogLevel level)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.AddProperty<T>(string propertyName, T value)
    {
        _properties.Add(new KeyValuePair<string, object?>(propertyName, value));
        AddProperty(propertyName, value);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.AddProperty<T>(string propertyName, T value)
    {
        _properties.Add(new KeyValuePair<string, object?>(propertyName, value));
        AddProperty(propertyName, value);
        return this;
    }

    /// <inheritdoc/>
    void IOperationLogBase.AddProperty<T>(string propertyName, T value)
    {
        _properties.Add(new KeyValuePair<string, object?>(propertyName, value));
        AddProperty(propertyName, value);
    }

    /// <summary>
    /// Does nothing. Called by all three explicit implementations - <see cref="IOperationLog.AddProperty{T}"/>,
    /// <see cref="ISubOperationLog.AddProperty{T}"/>, and <see cref="IOperationLogBase"/>'s own copy - after
    /// the property has already been recorded in <c>Properties</c>, so a test can set this up or verify it
    /// in place of any of them - overriding it does not prevent the property from being recorded. Unlike
    /// most other shared members, <see cref="IOperationLogBase"/> needs its own explicit implementation
    /// here rather than being satisfied implicitly by this virtual method - see the remarks on
    /// <see cref="FakeOperationLog"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    public virtual void AddProperty<T>(string propertyName, T value)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.AppendException(Exception exception)
    {
        AppendException(exception);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both
    /// <see cref="IOperationLog.AppendException"/>/<see cref="ISubOperationLog.AppendException"/>
    /// implementations, so a test can set this up or verify it in place of either.
    /// </summary>
    /// <param name="exception">Ignored.</param>
    public virtual void AppendException(Exception exception)
    {
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.AppendResult<T>(T value)
    {
        AppendResult(value);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both
    /// <see cref="IOperationLog.AppendResult{T}"/>/<see cref="ISubOperationLog.AppendResult{T}"/>
    /// implementations, so a test can set this up or verify it in place of either.
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
    IOperationLog IOperationLog.Append(string text)
    {
        Append(text);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.Append(string text)
    {
        Append(text);
        return this;
    }

    /// <inheritdoc/>
    IOperationLog IOperationLog.Append([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler text)
    {
        Append(TakeText());
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.Append(ref OperationLogInterpolatedStringHandler text)
    {
        Append(TakeText());
        return this;
    }

    /// <inheritdoc/>
    void IOperationLogBase.Append(ref OperationLogInterpolatedStringHandler text) =>
        Append(TakeText());

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
    IOperationLog IOperationLog.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    /// <inheritdoc/>
    ISubOperationLog ISubOperationLog.AppendValue<T>(T value, string? valueName)
    {
        AppendValue(value, valueName);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both
    /// <see cref="IOperationLog.AppendValue{T}"/>/<see cref="ISubOperationLog.AppendValue{T}"/>
    /// implementations, so a test can set this up or verify it in place of either.
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
    IOperationLog IOperationLog.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    ISubOperationLog ISubOperationLog.AppendJson<T>(T value, string? valueName)
    {
        AppendJson(value, valueName);
        return this;
    }

    /// <summary>
    /// Does nothing. Called by both
    /// <see cref="IOperationLog.AppendJson{T}"/>/<see cref="ISubOperationLog.AppendJson{T}"/>
    /// implementations, so a test can set this up or verify it in place of either.
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

    /// <summary>
    /// Returns the <see cref="ISubOperationLog"/> to hand out as the sub-operation named
    /// <paramref name="operationName"/>. Unlike every other member here, there's no sensible do-nothing
    /// default - a caller that begins a sub-operation always needs some <see cref="ISubOperationLog"/> back
    /// - so this is abstract rather than a virtual no-op: override it (or set it up on a mock) to say what
    /// the sub-operation actually is. Returning <see langword="this"/> reproduces the old default (a
    /// "sub-operation" of this fake is this same instance); building a new instance with the
    /// <see cref="FakeOperationLog(FakeOperationLog)"/> constructor, passing the instance
    /// <see cref="BeginSubOperation(string)"/> was called on, gets a genuinely separate
    /// <see cref="ISubOperationLog"/> - with its own <see cref="Dispose()"/> - that still shares
    /// <c>Properties</c> the way a real sub-operation does. Any other <see cref="ISubOperationLog"/> - a
    /// mock, whatever the test needs to assert against - works just as well.
    /// </summary>
    /// <param name="operationName">The name the caller wrote (already interpolated, if that overload was used).</param>
    /// <returns>The <see cref="ISubOperationLog"/> representing the nested sub-operation.</returns>
    public abstract ISubOperationLog BeginSubOperation(string operationName);

    /// <inheritdoc/>
    ISubOperationLog IOperationLogBase.BeginSubOperation([InterpolatedStringHandlerArgument("")] ref OperationLogInterpolatedStringHandler operationName) =>
        BeginSubOperation(TakeText());

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
    /// <see cref="IDisposable.Dispose"/> above, which satisfies <see cref="IOperationLog"/>'s,
    /// <see cref="ISubOperationLog"/>'s, and <see cref="IOperationLogBase"/>'s <see cref="IDisposable"/> at
    /// once (all three declare a plain <see langword="void"/> <c>Dispose()</c>, so - unlike the other
    /// fluent members - no per-interface split is needed). If <see cref="BeginSubOperation(string)"/> is
    /// overridden (or set up) to hand back this same instance, disposing a "sub-operation" invokes this
    /// exact override too - see the remarks on <see cref="FakeOperationLog"/>.
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
