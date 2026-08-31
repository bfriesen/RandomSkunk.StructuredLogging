# <img src="https://raw.githubusercontent.com/bfriesen/RandomSkunk.StructuredLogging/main/icon.png" alt="" width="32" height="32" valign="middle" /> RandomSkunk.StructuredLogging

[![NuGet](https://img.shields.io/nuget/v/RandomSkunk.StructuredLogging.svg)](https://www.nuget.org/packages/RandomSkunk.StructuredLogging)
[![CI](https://github.com/bfriesen/RandomSkunk.StructuredLogging/actions/workflows/ci.yml/badge.svg)](https://github.com/bfriesen/RandomSkunk.StructuredLogging/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/bfriesen/RandomSkunk.StructuredLogging/blob/main/LICENSE)

Modern structured logging extensions for .NET that separate human-readable messages from
machine-readable attributes.

`Microsoft.Extensions.Logging`'s `LogInformation`/`LogDebug`/`LogWarning`/etc. extension methods
force every structured property into the message template:

```csharp
logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

That's fine until the message needs to read differently than the template dictates, or a property
needs to be attached without cluttering the sentence. This library separates the two concerns: the
message is a normal interpolated string, worded however you want, and structured properties are
attached explicitly alongside it.

```csharp
using RandomSkunk.StructuredLogging;

logger.Information($"User logged in", ("UserId", userId), ("IpAddress", ipAddress));
```

Alongside these per-call log methods, the library also has an [operation logging](#operation-logging)
feature: journal everything that happens during an operation (and any nested sub-operations) and
flush it as exactly one log entry when the operation completes, instead of one log line per step.

## Install

```bash
dotnet add package RandomSkunk.StructuredLogging
```

Targets `net8.0` and `net10.0`, and depends only on `Microsoft.Extensions.Logging.Abstractions`
(8.0.0 or newer on `net8.0`, 10.0.0 or newer on `net10.0`). The assemblies are not strong-named.

## The basic call

One extension method per `LogLevel` — `Trace`, `Debug`, `Information`, `Warning`, `Error`,
`Critical` — plus a level-as-argument `Write`. The message parameter accepts a normal interpolated
string:

```csharp
logger.Information($"User {userId} logged in");
logger.Write(LogLevel.Information, $"User {userId} logged in");
```

Each level method (and `Write`) has overloads for the leading `EventId`/`Exception` arguments —
both, either, or neither — always in this order: `logger`, `[eventId]`, `[exception]`, `message`,
...:

```csharp
logger.Information(MyEventIds.UserLoggedIn, $"User {userId} logged in");        // EventId only
logger.Error(ex, $"Failed to process order {orderId}");                         // Exception only
logger.Error(MyEventIds.OrderFailed, ex, $"Failed to process order {orderId}"); // both
```

You don't need to guard these calls with `if (logger.IsEnabled(LogLevel.X))`. The message
parameter is a custom `[InterpolatedStringHandler]`, not a plain `string` — its constructor checks
`IsEnabled` before any interpolation hole is evaluated, so `logger.Debug($"... {ExpensiveCall()}")`
never calls `ExpensiveCall()` when Debug is disabled. A plain `string` argument (no interpolation)
is also accepted and always treated as enabled, so a hardcoded or precomputed message is never
penalized either.

## Attaching structured properties

There are four ways to attach properties to a call, chosen by what you know at the call site.

### 1. Statically-known properties (up to 6)

Pass one `(string Name, T Value)` tuple argument per property, after the message. These generic
overloads avoid boxing the values until (and unless) a log provider actually reads them:

```csharp
logger.Information($"Order processed", ("OrderId", orderId), ("Total", total));
logger.Warning(MyEventIds.SlowRequest, $"Slow request", ("Path", path), ("DurationMs", elapsedMs));
```

### 2. `<PropertyName>` format tags — capture a value that's also in the message

An interpolation hole's format can start with an "html-like" tag to append the value to the
message text *and* capture its raw, unformatted value as a structured property under that name:

```csharp
logger.Information($"User {userId:<UserId>} logged in from {ip:<IpAddress>}");
// message text:         "User 42 logged in from 10.0.0.1"
// structured properties: UserId = 42 (int), IpAddress = <IPAddress instance>

logger.Debug($"[{ts:<Timestamp>HH:mm:ss}] tick");
// message text uses "HH:mm:ss" to format ts; the Timestamp property holds the raw DateTime
```

An empty tag (`<>`) strips itself out without capturing anything — it's only useful when *both* of
these are true: you don't want to capture the value, *and* your real format string happens to start
with `<`. If your real format doesn't start with `<`, just write it directly with no `<>` prefix —
`{value:therealformat}` already opts out of capturing on its own. The escape hatch exists purely to
disambiguate a leading `<` in a real format from a capture tag: `{value:<>therealformat}`.

A tag that starts with `<` but has no closing `>` (e.g. `{value:<UserId}`, a missing `>` typo)
throws `UnterminatedLogPropertyTagException` (a `FormatException`) at the point the interpolation
hole is appended, rather than silently treating `<UserId` as a real format string handed to
`value`'s `IFormattable.ToString(format)`. If you genuinely need a format that starts with `<`,
use the `<>` escape hatch shown above. The analyzers package catches this at compile time instead —
see `RSSL0008` below.

#### `<@PropertyName>` — Serilog-style destructured capture

A tag whose name starts with `@` requests Serilog-style destructured capture. With no format after
the tag, the value is also rendered into the *message text* using destructured formatting instead
of `ToString()`/`IFormattable` formatting:

```csharp
logger.Trace($"Item added to cart: {item:<@Item>}");
// message text:          "Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }"
// structured properties: @Item = <the raw OrderItem instance>

logger.Trace($"Item added to cart: {item:<@>}");
// same message text, but the empty destructuring tag doesn't capture a structured property
```

The captured property's key gets an `@` prefix (Serilog's own destructuring operator, distinct
from this library's tag syntax) so a Serilog-style sink knows to destructure the raw value at
processing time — the captured *value* is always the raw, undestructured object either way; only
the key changes.

A format after the tag is honored for the message exactly like an ordinary `<PropertyName>` tag,
decoupling how the value looks in the message from how the sink should capture it:

```csharp
logger.Information($"Total: {amount:<@Amount>F3}");
// message text:          "Total: 3.142"                      (amount.ToString("F3"), not destructured)
// structured properties: @Amount = <the raw amount value>    (still flagged for the sink to destructure)
```

The empty destructuring tag (`<@>`) is only useful when you don't want to capture the value but
still want it *rendered into the message* using destructured formatting rather than
`ToString()`/`IFormattable`. If you don't want destructured rendering either, skip the tag
entirely. A format after an empty destructuring tag (`<@>F3`) behaves exactly like a plain `<>F3`
— no capture (there's no property name to prefix), format the message with `F3` — since once
there's no name, the `@` has nothing left to attach destructuring semantics to for capture
purposes.

Rendering rules: objects render as `TypeName { Prop1: Value1, Prop2: Value2 }` (anonymous types
omit the type name); collections render as `[item1, item2]`; dictionaries render as
`{ [key1]: value1, [key2]: value2 }`; strings and chars are quoted, other scalars (numbers,
`bool`, enums, `DateTime`, `Guid`, etc.) render unquoted using invariant culture; `null` renders as
`null`. Nested objects/collections are capped at 10 levels deep and 10 items per
collection/dictionary (both shown as `...` when exceeded), and a self-referencing object renders
`<circular reference>` instead of recursing forever.

### 3. Dynamic properties, or more than 6 — build a collection

Every generic-tuple call in option 1 is really calling one specific overload per arity (1–6);
there's no arbitrary-count `params` overload. Once a call needs more than 6 properties, or the set
of properties is built at runtime, build a collection instead and pass it via option 4 below:

```csharp
Dictionary<string, object?> properties = new()
{
    ["OrderId"] = orderId,
    ["Total"] = total,
    ["Currency"] = currency,
    ["Tax"] = tax,
    ["Discount"] = discount,
    ["Shipping"] = shipping,
    ["CouponCode"] = coupon,
};

logger.Information(properties, $"Order processed");
```

### 4. Merging a pre-built collection with per-call properties

If you already have an `IReadOnlyCollection<KeyValuePair<string, object?>>` (say, a
`Dictionary<string, object?>` of ambient/scope properties), every shape above has a sibling
overload that accepts it as a leading parameter, right after `logger` (before `eventId`/`exception`
if present):

```csharp
logger.Warning(scopeProperties, MyEventIds.SlowRequest, $"Slow request to {path:<Route>}", ("DurationMs", elapsedMs));
```

Properties end up in this order: the collection's entries, then tag-captured properties, then the
trailing per-call properties argument(s).

A collection that is already an `IReadOnlyList<KeyValuePair<string, object?>>` — such as a
`List<KeyValuePair<string, object?>>` — is used exactly as passed. Anything else, `Dictionary<string,
object?>` included, is copied into an array once per call, since the properties have to be
addressable by index. The copy is a single small array and rarely worth thinking about, but if
you're building the collection yourself on a hot path, building a `List` instead of a `Dictionary`
avoids it entirely.

All four options can be combined freely in a single call — tags in the message, static tuples
after it, and (if needed) a leading collection — as long as the total structured property count
stays within what the chosen overload supports.

## Design goals

- **Messages are just strings.** No format-string parsing or caching at the framework level — a
  message can be as dynamic or as constant as you like without a performance penalty either way.
- **Properties are explicit and typed.** Statically-known properties are passed as generic tuples
  and stay unboxed until a provider reads them; only the `params`/collection overloads box.
- **`IsEnabled` checks are automatic and free.** Expensive interpolation holes are never evaluated
  when the level is disabled, without writing a guard clause yourself.
- **No `IFormatProvider` surprises.** Interpolated message text is formatted with
  `CultureInfo.InvariantCulture`, matching how most structured log sinks expect it.

## Migrating from `Microsoft.Extensions.Logging`

```csharp
// Before:
logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);

// After — properties still shown in the message text:
logger.Information($"User {userId:<UserId>} logged in from {ipAddress:<IpAddress>}");

// After — message worded freely, properties attached separately:
logger.Information($"User logged in", ("UserId", userId), ("IpAddress", ipAddress));
```

```csharp
using Microsoft.Extensions.Logging;      // for LogLevel, EventId, ILogger, etc.
using RandomSkunk.StructuredLogging;     // brings the Trace/Debug/.../Write extension methods into scope
```

## Common pitfalls

The "never evaluate interpolation holes when the level is disabled" optimization, and `<PropertyName>`
tag capture, both depend on the compiler binding your `message` argument to this library's
`[InterpolatedStringHandler]` overload instead of the plain `string` overload. That binding only
happens when the argument is *statically*, syntactically an interpolated-string literal (`$"..."`)
at the call site. Anything that forces the value through `string` first gets none of it - the
interpolation runs unconditionally, eagerly, exactly as expensive as it would be with
`Microsoft.Extensions.Logging`'s `LogInformation`/etc.:

```csharp
logger.Debug($"User {name}".ToUpper());          // a method call on the literal forces `string`
logger.Debug($"count: " + count);                // string concatenation, same problem

string message = $"User {userId} logged in";     // interpolated string assigned to a local first
logger.Debug(message);                            // ...then passed as a plain `string`

logger.Debug(FormatMessage($"User {userId} logged in")); // passed through a wrapper/helper method
```

None of these fail to compile, throw, or log anything different-looking - they just silently give
up the performance benefit, and any `<PropertyName>` tags in the format specifiers are no longer
parsed as tags at all (see below for what they become instead).

**The local-variable and wrapper-method forms are worse than just "no benefit" - they're a runtime
bug if any hole uses a `<PropertyName>` tag.** Once the interpolated string is built by the ordinary
compiler-provided handler instead of this library's, `<Name>` (or `<@Name>`) is passed straight
through as a genuine .NET format string to that value's `IFormattable.ToString(format)`. Most types
don't recognize `"<Name>"` as a valid format - this throws `FormatException` as soon as that code
path actually executes (e.g., someone enables Debug logging in production to investigate an issue,
and the app starts throwing from the log statement itself):

```csharp
string message = $"User {userId:<UserId>} logged in"; // throws FormatException once Debug logging
logger.Debug(message);                                  // is enabled and this line actually runs

logger.Debug(FormatMessage($"User {userId:<UserId>} logged in")); // same failure, one call away -
                                                                     // FormatMessage's parameter is
                                                                     // just `string`, so the literal
                                                                     // is built by the ordinary
                                                                     // handler when this line runs
```

The fix in every case is the same: pass the interpolated string literal directly as the `message`
argument, rather than through a variable, method call, concatenation, or helper method. The
`InterpolatedStringLocalMessageAnalyzer` (`RSSL0007`) catches the local-variable form at compile
time, the `InterpolatedStringMessageExpressionAnalyzer` (`RSSL0009`) catches the method-call/
concatenation form, and the `InterpolatedStringHelperMethodArgumentAnalyzer` (`RSSL0010`) catches
the wrapper/helper-method form - see below.

## Operation logging

Beyond individual log calls, the library also has a canonical-log-line-style feature for
journaling everything that happens during an operation and flushing it as exactly **one** log
entry when the operation completes — instead of one log line per step, scattered across the
timeline and hard to correlate.

```csharp
using var log = logger.BeginOperation("FulfillOrder");

log.AddProperty("OrderId", orderId);
log.Append($"Validating order {orderId}");

using (var paymentLog = log.BeginSubOperation("ChargePayment"))
{
    try
    {
        var receipt = await paymentGateway.ChargeAsync(orderId);
        paymentLog.AppendResult(receipt);
    }
    catch (Exception ex)
    {
        paymentLog.AppendException(ex);
        throw;
    }
}

return order.SetResultTo(log);
```

Disposing `log` writes a single log entry whose message *is* the full journal - every
`Append`/`AppendValue`/`AppendJson` call, sub-operation start/result/failure/complete line, and
`SetException`/`SetResult` marker, each timestamped with elapsed seconds - and whose structured
properties include `Operation.Name`, `Operation.StartTime`, `Operation.DurationSeconds`,
`Operation.Result` (if set), and any properties added via `AddProperty`. The journal's header starts
with the operation name; if `BeginOperation` was called with a non-default `EventId`, the next header
line shows its value; then a `Start Time` line; then a `Properties:` line followed by a bulleted list
naming every structured property the final entry will carry - the built-ins above plus any added via
`AddProperty` - ending with a dashed rule:

```
Operation: FulfillOrder
Start Time: 2026-08-19 12:56:31.417 -04:00
Properties:
- Operation.Name
- Operation.StartTime
- Operation.DurationSeconds
- Operation.Result
- OrderId
----------------------------------------
[0.002] Validating order 42
[0.003] `ChargePayment` started.
[0.041] `ChargePayment` result: Receipt { Id = ..., Amount = 99.00 }
[0.041] `ChargePayment` complete.
[0.042] Operation result set.
[0.042] Operation complete.
```

If the logger's level is disabled, `BeginOperation` returns an `IOperationLog` that never writes a
log entry and never accumulates a journal - no need to guard the call yourself. `EventId` and
`Properties` (via `AddProperty`) still behave normally even when disabled, since code may read them
regardless of whether the operation ends up logging anything; `IsEnabled` reports `false` so you can
check that yourself before doing work that would otherwise go to waste.

### API at a glance

- `logger.BeginOperation(name)` / `logger.BeginOperation(eventId, name)` - both take optional
  `level` (default `LogLevel.Information`) and `threadSafe` (default `false`) parameters, and
  return the root `IOperationLog`. `IOperationLog.BeginSubOperation(name)` starts a nested
  sub-operation, returned as a separate `ISubOperationLog`. `IOperationLog` and `ISubOperationLog`
  are unrelated to each other - neither extends the other - so a chain of calls on one keeps
  returning that same interface rather than widening to a shared ancestor, and so a sub-operation
  reference can never reach the root-only members below. Both extend a shared, non-fluent
  `IOperationLogBase` interface for everything else - useful mainly for writing code that works
  against either kind of operation log at the cost of chaining - but listed here as members of
  `log` regardless of which concrete interface it is, since `IOperationLog`/`ISubOperationLog` each
  redeclare their own fluent, self-returning version of every member below.
- `log.AddProperty<T>(name, value)` - adds a structured property to the final entry. Available on
  the root and every sub-operation.
- `log.Properties` - the properties added so far via `AddProperty`, as an
  `IReadOnlyList<KeyValuePair<string, object?>>`, readable while the operation is still open (the
  final entry isn't written until `Dispose`). The root and every sub-operation share the same
  list, since they all contribute to the one eventual entry. Under `threadSafe: true`, this
  returns a point-in-time snapshot rather than a live view, so it's safe to enumerate even while
  another thread is concurrently calling `AddProperty`.
- `log.EventId` - the `EventId` the operation was begun with (via
  `BeginOperation(eventId, name, ...)`), or `default` if none was given. Same value on the root
  and every sub-operation. Useful for tagging a log line written elsewhere - e.g. from within the
  operation, or from code the operation called into - with the same `EventId` as the operation's
  own final entry, so the two can be correlated in a backend that indexes/filters by `EventId`.
- `log.IsEnabled` - whether the operation is actually journaling, i.e. whether the level
  passed to `BeginOperation` was enabled on the logger at the time. Same value on the root and
  every sub-operation, and never changes afterward - not even `Escalate` can turn a disabled
  operation into an enabled one. Useful for skipping work that only feeds an `AddProperty`/
  `AppendValue`/`AppendJson` call, e.g. `if (log.IsEnabled) log.AppendJson(BuildExpensiveDiagnostics());`.
- `log.Append(text)` - appends a free-text line to the journal. `text` can be a plain
  `string` or an interpolated string (`$"..."`); an interpolated argument's holes are only
  evaluated when `IsEnabled` is `true`, same as the structured-logging message parameters, and -
  when the operation isn't `threadSafe: true` - are written straight into the journal instead of
  building a separate string first.
- `log.AppendValue<T>(value, [valueName])` - appends `` `valueName`: value ``, where
  `valueName` defaults to the value expression's source text (via `CallerArgumentExpression`), so
  `log.AppendValue(order.Total)` appends `` `order.Total`: 42.50 `` with no name to spell out.
- `log.AppendJson<T>(value, [valueName])` - same as `AppendValue`, but `value` is
  rendered as indented JSON instead of via `ToString()`/`IFormattable`.
- `log.BeginSubOperation(name)` - starts a nested `ISubOperationLog`, immediately
  appending a "started" line to the journal; disposing it appends a "complete" line. Sub-operations
  never write their own log entry - only the root operation does, once, when *it's* disposed.
  Always returns `ISubOperationLog`, even when called on another sub-operation. `name` can be a
  plain `string` or an interpolated string, with the same disabled-skips-evaluation behavior as
  `Append`.
- `log.AppendException(exception)` - appends a "failed" line to the journal describing
  `exception` (`` `name` failed: ... `` on a sub-operation, `Operation failed: ...` on the root).
  Available on the root and every sub-operation. Unlike `IOperationLog.SetException` (below), this
  never sets the final entry's `Exception` property by itself.
- `log.AppendResult<T>(value)` - appends a result line to the journal (`` `name` result: ... `` on
  a sub-operation, `Operation result: ...` on the root). Available on the root and every
  sub-operation. Unlike `IOperationLog.SetResult` (below), this never sets the `Operation.Result`
  structured property by itself. Typically called via the fluent `value.AppendResultTo(log)`
  extension method so it can be chained directly onto a `return` expression.
- `log.Escalate(level)` - raises the level the final entry is written at, if `level` is
  more severe than the operation's current level; otherwise a no-op. Never re-enables an operation
  whose level was disabled up front at `BeginOperation`. When it actually raises the level, it also
  appends a journal line naming both levels - `Operation escalated from Information to Warning.` on
  the root, `` `PaymentCheck` escalated from Information to Warning. `` on a sub-operation - so the
  final entry's level is self-explanatory without having to search the rest of the journal for why.
  Useful even without an exception, e.g. a rejected/backordered/declined result that should still
  raise the log level: `log.Escalate(LogLevel.Warning);`.
- `IOperationLog.SetException(exception)` - **root only**; not on `ISubOperationLog`. Sets the
  final log entry's `Exception` structured property directly. Unlike `AppendException` above, it
  doesn't write the exception's text to the journal - it appends a one-line `Operation exception
  set.` marker instead (`Operation exception set again, overwriting the previous value.` on a
  second or later call, so an accidental double-set is visible in the journal instead of silently
  overwriting the first exception). A sub-operation that wants to fail the root's own entry holds
  onto the root `IOperationLog` (not the sub-operation) and calls this on it directly; otherwise
  use `AppendException` above.
- `IOperationLog.SetResult<T>(value)` - **root only**; not on `ISubOperationLog`. Sets the
  `Operation.Result` structured property directly. Unlike `AppendResult` above, it doesn't write
  the formatted value to the journal - it appends a one-line `Operation result set.` marker instead
  (`Operation result set again, overwriting the previous value.` on a second or later call). Typically
  called via the fluent `value.SetResultTo(log)` extension method (root only - a sub-operation
  uses `AppendResultTo` above instead) so it can be chained directly onto a `return` expression.
- `value.AppendValueTo(log, [valueName])` / `value.AppendJsonTo(log, [valueName])` /
  `value.AddPropertyTo(log, name)` - fluent equivalents of `AppendValue`/`AppendJson`/
  `AddProperty` that return `value` unchanged, for chaining inline into an expression, e.g.
  `var total = order.Total.AppendValueTo(log);`. Each works whether `log` is an `IOperationLog` or
  an `ISubOperationLog`.

Every method returns the exact interface it was called on, so calls can be chained:
`log.AddProperty("OrderId", orderId).Append("Order validated");` keeps returning `IOperationLog`
when `log` is the root, and `ISubOperationLog` when it's a sub-operation.

`log.Properties` implements `IReadOnlyCollection<KeyValuePair<string, object?>>`, so it can be
passed directly as the leading collection argument (option 4 above) to an ordinary structured log
call, and `log.EventId` alongside it - useful for surfacing the operation's context on a
standalone log line emitted mid-operation, before the operation's own entry is flushed, while
still tying that line back to the operation via a shared `EventId`:

```csharp
if (elapsed > paymentGateway.SlowThreshold)
    logger.Warning(log.Properties, log.EventId, $"Payment gateway is responding slowly ({elapsed:<ElapsedMs>ms})");
```

### Thread safety

Operation logs are **not thread-safe by default** - if a single operation's sub-operations run
concurrently (e.g. via `Task.WhenAll`), pass `threadSafe: true` to `BeginOperation` to wrap the
entire tree (root and every nested sub-operation) in a decorator that synchronizes on one shared
lock:

```csharp
using var log = logger.BeginOperation("ProcessBatch", threadSafe: true);

await Task.WhenAll(items.Select(async item =>
{
    using var subLog = log.BeginSubOperation($"Item {item.Id}");
    await ProcessAsync(item);
}));
```

### Testing code that takes an `IOperationLog`

Two recipes, depending on what the code under test needs.

**A real, silent operation log.** If the code just needs *an* operation log and the test doesn't care
what it records, begin one on `NullLogger.Instance`. The level is disabled, so you get the library's
own no-op implementation - no journal, no log entry - while `EventId` and `Properties` still behave
exactly as they would on an enabled operation:

```csharp
using IOperationLog log = NullLogger.Instance.BeginOperation("Test");
```

**A test double you can assert on.** Don't mock `IOperationLog`/`ISubOperationLog` directly.
`Append` and `BeginSubOperation` each have an overload taking an interpolated string handler - a
`ref struct` parameter that Moq, NSubstitute, and anything else built on Castle DynamicProxy can't
forward. The proxy generated for those two members is invalid, so `log.Append($"...")` throws
`InvalidProgramException` at run time; since that's the idiomatic call, a mock of the interface fails
on contact with the code it's meant to test.

Depend on `OperationLogFactory<TCategoryName>` instead of calling `logger.BeginOperation(...)`
directly. It mirrors `ILogger<TCategoryName>`, so declare it closed over the consuming type exactly as
you would `ILogger<TSelf>`:

```csharp
public sealed class OrderShipper
{
    private readonly OperationLogFactory<OrderShipper> _operationLogFactory;

    public OrderShipper(OperationLogFactory<OrderShipper> operationLogFactory) => _operationLogFactory = operationLogFactory;

    public void Ship(int orderId)
    {
        using IOperationLog log = _operationLogFactory.BeginOperation("ShipOrder");
        log.AddProperty("OrderId", orderId);
        log.Append($"Fetching order {orderId}");
        // ...
        log.SetResult("Shipped");
    }
}
```

`BeginOperation` (and its `EventId`-taking overload) is `virtual`, so a test mocks the factory to
return `FakeOperationLog` - a single do-nothing type implementing both `IOperationLog` and
`ISubOperationLog` - in place of a real operation:

```csharp
Mock<OperationLogFactory<OrderShipper>> factory = new(NullLogger<OrderShipper>.Instance);
Mock<FakeOperationLog> log = new();
factory.Setup(x => x.BeginOperation("ShipOrder", It.IsAny<LogLevel>(), It.IsAny<bool>())).Returns(log.Object);

new OrderShipper(factory.Object).Ship(orderId: 42);

log.Verify(x => x.Append("Fetching order 42"), Times.Once);
log.Verify(x => x.SetResult("Shipped"), Times.Once);
```

No `CallBase` needed for `Append`, `AddProperty`, `Escalate`, `AppendException`, `AppendResult`,
`AppendValue`, `AppendJson`, `SetException`, and `SetResult`: each has a mockable `void`-returning
counterpart with the same name, so an unconfigured mock simply does nothing rather than returning
`null` for the next call in the chain to fail on. The actual `return this` that keeps a fluent chain
going lives in a non-mockable explicit interface implementation, which always runs for real.

`BeginSubOperation` is the exception - `FakeOperationLog` is `abstract`, and `BeginSubOperation` is its
one abstract member. `Ship` above never begins a sub-operation, so the mock doesn't need it configured;
code that does call `BeginSubOperation` does, though, since there's no sensible do-nothing default for
"what sub-operation log should this return" - unlike every other member, it has to be configured (or
overridden in a subclass) first:

```csharp
log.Setup(x => x.BeginSubOperation(It.IsAny<string>())).Returns(log.Object);
```

Left unconfigured, `Mock<FakeOperationLog>` returns `null` for it, and NSubstitute's
`Substitute.ForPartsOf<>` auto-generates a raw `ISubOperationLog` substitute to return instead - which
throws the exact `InvalidProgramException` this fake exists to avoid, the moment code under test calls
its interpolated `Append` overload. Returning the fake itself, as above, reproduces the old
always-`this` behavior; returning a different `ISubOperationLog` (another `FakeOperationLog`, a
separately-asserted mock) works just as well when the test needs to tell the sub-operation apart from
the root.

`IsEnabled` is always `true` on `FakeOperationLog` and can't be overridden or mocked - this fake has no
disabled state, so its interpolated `Append`/`BeginSubOperation` overloads always evaluate their holes.
To test code against a genuinely disabled operation, use the first recipe above (`NullLogger.Instance`)
instead.

When `BeginSubOperation` is configured (or overridden) to return the fake itself, a "sub-operation"
shares `Properties`/`EventId` with the root exactly as a real one does - but disposing either disposes
the same object, so don't expect independent `Dispose` calls per sub-operation. A subclass that wants
independent `Dispose` calls *and* correctly shared `Properties` can have `BeginSubOperation` return a
new instance built with `FakeOperationLog`'s protected copy constructor instead:

```csharp
private sealed class TestOperationLog : FakeOperationLog
{
    public TestOperationLog()
    {
    }

    private TestOperationLog(TestOperationLog parent) : base(parent)
    {
    }

    public override ISubOperationLog BeginSubOperation(string operationName) => new TestOperationLog(this);
}
```

The new instance's `Properties` list is the same list, by reference, as the instance it was built
from - so `AddProperty` calls made through either are visible through both, exactly as they would be on
a real operation and its sub-operation - but everything else (the journal buffer, `Dispose`) is
independent.

In production, register `OperationLogFactory<>` once, as an open generic, alongside your usual
`AddLogging()` call:

```csharp
services.AddSingleton(typeof(OperationLogFactory<>));
```

A container that already resolves `ILogger<TCategoryName>` - every container built on
`Microsoft.Extensions.DependencyInjection` does, once logging is registered - resolves
`OperationLogFactory<TCategoryName>` for any consuming type from that one registration, with nothing
further to wire up per type.

If you'd rather exercise a real, enabled operation against a fake or mocked `ILogger` you control -
instead of mocking `OperationLogFactory` itself - expect exactly **one** call to that logger once the
operation is disposed, no matter how many `Append`/`AddProperty`/sub-operation calls happened along the
way. That's the whole point of the feature: everything collapses into a single log entry on dispose, in
a test double exactly as it would in a real sink.

What's safe to assert against that one call is its structured properties: `Operation.Name`,
`Operation.StartTime`, and `Operation.DurationSeconds` are always present; `Operation.Result` is present
only if `SetResult` was called; and anything added via `AddProperty` is there under the name it was
added with. There's no `Operation.Exception` property - an exception set via `SetException` is carried
as the log entry's own `Exception` argument instead (a hand-rolled `ILogger` test double's own field for
it, or the exception argument captured from a mocked `Log<TState>` call), the same place any ordinary
log call's exception would be. The entry's `LogLevel` and `EventId` behave normally too, exactly as they
would for a non-operation log call. The journal text making up the message, on the other hand, is *not*
part of the stable contract - its formatting (headers, timestamps, the dashed rule, journal line
wording) can change between versions, so avoid asserting against more of it than the specific
substrings your own `Append`/`AppendValue`/`AppendJson` calls contributed.

### Common pitfalls

`BeginOperation`/`BeginSubOperation` return an ordinary `IDisposable` - nothing enforces disposal.
Forget the `using`, and the operation's *entire* journal is silently dropped: not even a partial
log entry is written, no exception is thrown, and the pooled `StringBuilder` the journal was
building into never returns to the pool. This is easy to miss because it doesn't look dangerous -
unlike a leaked file handle or connection, there's no resource exhaustion to notice until you go
looking for a log entry that should be there and find nothing.

[CA2000](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2000) won't
catch this for you: its escape analysis anchors on `new`-expressions visible in your code, but
`BeginOperation`'s internal `new RootOperationLog(...)`/`new DisabledOperationLog(...)` live inside
the already-compiled library assembly, opaque to CA2000. Since you only ever obtain an
`IOperationLog` from `BeginOperation`/`BeginSubOperation` (never `new`), CA2000 essentially never
fires here. This library ships its own analyzer for exactly this gap - see `RSSL0006` below - which
flags an undisposed `IOperationLog` and offers a code fix to wrap it in a `using` declaration or
block. As always, `using var log = logger.BeginOperation(...);` is the simplest way to avoid the
problem in the first place.

A sub-operation's `ISubOperationLog` only ever appends to the root operation's shared journal - it
never writes its own log entry. If a sub-operation reference outlives the root (e.g. it's leaked
out of scope, or held by a fire-and-forget task), any attempt to use it - `AddProperty`, `Append`,
`AppendValue`, `AppendJson`, `BeginSubOperation`, `AppendException`, `AppendResult`, or `Dispose` -
throws `ObjectDisposedException` once the root has been disposed, rather than silently mutating a
pooled `StringBuilder` that may already have been handed out to a completely different operation
elsewhere in the app.

## Trimming and Native AOT

The library is marked trim-safe (`IsAotCompatible`), and every logging call, structured property,
and operation-logging member is safe to use from a trimmed or Native AOT application - with one
exception, plus one caveat.

**`AppendJson` is not AOT safe.** `IOperationLog.AppendJson`/`ISubOperationLog.AppendJson` (and the
`AppendJsonTo` extension) serialize an arbitrary value with reflection-based `System.Text.Json`,
which throws in a Native AOT app and can lose members under trimming. They're annotated
`[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`, so calling them from a trimmed or AOT project
produces build warnings `IL2026`/`IL3050` rather than failing silently at run time. Use
`AppendValue` instead, or preserve the serialized type.

**Destructuring degrades gracefully.** The [`<@PropertyName>` destructuring tag](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message)
reflects over the runtime type of whatever value it's given, which can't be statically analyzed. It
does *not* warn, because it can't break: a property the trimmer removed is simply absent from the
rendered text, the type's surviving properties still render, and nothing throws. If you need a
particular type rendered in full from a trimmed app, preserve it (`[DynamicDependency]`, a
`DynamicallyAccessedMembers` annotation on your own code, or an ILLink descriptor) - or attach the
value with `AddProperty`/a tuple argument instead of destructuring it into the message.

## Analyzers

[![NuGet](https://img.shields.io/nuget/v/RandomSkunk.StructuredLogging.Analyzers.svg)](https://www.nuget.org/packages/RandomSkunk.StructuredLogging.Analyzers)

```bash
dotnet add package RandomSkunk.StructuredLogging.Analyzers
```

`RandomSkunk.StructuredLogging.Analyzers` is a separate package of Roslyn analyzers that ship as
a build-time-only dependency (it adds nothing to your published output). Installing
`RandomSkunk.StructuredLogging` automatically brings it in as a dependency, so you get these
analyzers for free — the `dotnet add package` command above is only needed to install the
analyzers by themselves, in a project that doesn't reference `RandomSkunk.StructuredLogging`
itself (or doesn't need the runtime library at all, e.g. one that only calls
`Microsoft.Extensions.Logging` and wants RSSL0001's suggestion to switch).

| ID | Severity | Description |
| --- | --- | --- |
| <a id="rssl0001"></a>`RSSL0001` | Suggestion | Flags a call to one of `Microsoft.Extensions.Logging.LoggerExtensions`'s `Log`/`LogTrace`/`LogDebug`/`LogInformation`/`LogWarning`/`LogError`/`LogCritical` extension methods and suggests the equivalent `RandomSkunk.StructuredLogging` extension method — see [Migrating from `Microsoft.Extensions.Logging`](#migrating-from-microsoftextensionslogging) above. |
| <a id="rssl0002"></a>`RSSL0002` | Silent | Marks an interpolation hole that uses the [`<PropertyName>` tag format](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `{who:<Recipient>}`, including the destructuring `<@PropertyName>` form) to capture a structured property. Silent by default — it exists to anchor code fixes that act on these holes, not to warn about anything. |
| <a id="rssl0003"></a>`RSSL0003` | Silent | Marks an interpolation hole that does *not* use the [`<PropertyName>` tag format](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `{who}`) to capture a structured property. Silent by default — it exists to anchor code fixes that act on these holes, not to warn about anything. |
| <a id="rssl0004"></a>`RSSL0004` | Silent | Marks a name/value tuple argument (e.g. `("UserId", userId)`) passed at the end of a `RandomSkunk.StructuredLogging` extension method call to attach a structured property, when the name is a compile-time constant string (a literal, a constant concatenation, or an interpolated string whose holes are themselves constant strings). Silent by default — it exists to anchor code fixes that act on these arguments, not to warn about anything. |
| <a id="rssl0005"></a>`RSSL0005` | Silent | Marks a call to any of the `RandomSkunk.StructuredLogging` `Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical`/`Write` extension methods, regardless of overload. Its code fix rewrites the call to the roughly equivalent `Microsoft.Extensions.Logging` call (the inverse of `RSSL0001`), moving each structured property into a `{PropertyName}` message-template placeholder — since a hole that isn't already tagged with a name (including a bare `<@>` destructuring tag) gets one guessed from its expression (the same guess RSSL0003's fix uses), most calls convert cleanly. A call is left unconverted only when a name truly can't be pinned down: a hole whose expression isn't a simple identifier, a tuple argument with a dynamically-computed name, or the leading collection-parameter overload. Silent by default — it exists to anchor code fixes that act on these calls, not to warn about anything. |
| <a id="rssl0006"></a>`RSSL0006` | Warning | Flags a call to `BeginOperation`/`BeginSubOperation` whose returned `IOperationLog` isn't visibly disposed (via a `using` declaration/statement, an explicit `Dispose()` call, or by returning/assigning it elsewhere for someone else to dispose). Passing it as a plain method argument doesn't count — a sub-operation is expected to be created and disposed within the method it's threaded into, not handed off through a parameter. Forgetting to dispose it silently drops the entire journal — not even a partial log entry is written. CA2000 can't catch this itself, since its escape analysis anchors on `new`-expressions and can't see through `BeginOperation`'s internal object construction, which lives inside the already-compiled library assembly. |
| <a id="rssl0007"></a>`RSSL0007` | Warning | Flags a `RandomSkunk.StructuredLogging` call whose message argument is a local variable declared with an interpolated string initializer that captures at least one [`<PropertyName>` tag](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `string msg = $"User {id:<UserId>}"; logger.Debug(msg);`). Assigning the interpolated string to a `string` local first forces the call to bind the plain `string message` overload instead of the interpolated-string-handler overload — the message is then built eagerly regardless of level, and the tag is handed to the value's `IFormattable.ToString(format)` as a real (usually invalid) format string instead of being parsed as a property tag. Only fires when a tag is actually at stake; a tagless interpolated string local only loses the disabled-level evaluation optimization, which this analyzer doesn't police. Pass the interpolated string directly to the logging call instead. |
| <a id="rssl0008"></a>`RSSL0008` | Error | Flags an interpolation hole whose format specifier starts with `<` but has no matching `>` (e.g. `{userId:<UserId}`, a missing `>` typo) - almost always a mistake, since a real format that needs to start with a literal `<` should use the [`<>` escape hatch](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) instead. At run time this throws `UnterminatedLogPropertyTagException` rather than silently treating the raw text as a real format string; this analyzer catches the same mistake at compile time, at error severity since it always indicates a bug. |
| <a id="rssl0009"></a>`RSSL0009` | Warning | Flags a `RandomSkunk.StructuredLogging` call whose message argument applies a method call or `+` concatenation directly to an interpolated string literal that captures at least one [`<PropertyName>` tag](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `logger.Debug($"User {id:<UserId>}".ToUpper())` or `logger.Debug($"count: {count:<Count>}" + suffix)`). Either pattern forces the call to bind the plain `string message` overload instead of the interpolated-string-handler overload — the message is then built eagerly regardless of level, and the tag is handed to the value's `IFormattable.ToString(format)` as a real (usually invalid) format string instead of being parsed as a property tag. Only fires when a tag is actually at stake; a tagless interpolated string literal only loses the disabled-level evaluation optimization, which this analyzer doesn't police. Pass the interpolated string directly to the logging call instead. |
| <a id="rssl0010"></a>`RSSL0010` | Warning | Flags a `RandomSkunk.StructuredLogging` call whose message argument is the result of calling some other wrapper/helper method that was itself handed an interpolated string literal capturing at least one [`<PropertyName>` tag](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) as one of its arguments (e.g. `logger.Debug(FormatMessage($"User {id:<UserId>}"))`). Unless that helper method is itself written with an `[InterpolatedStringHandler]` parameter, the literal is built eagerly by the ordinary compiler-provided handler as soon as the helper is called, and whatever plain string the helper returns then binds the logging call's plain `string message` overload — with the same eager-evaluation and tag-as-real-format-string consequences as `RSSL0009`. Only fires when a tag is actually at stake, same as `RSSL0007`/`RSSL0009`. Pass the interpolated string directly to the logging call instead. |

### Code fixes

Most of the value of the analyzers package is in its code fixes (lightbulb ⚡ actions in the
IDE), several of which are offered even on the silent `RSSL0002`–`RSSL0005` diagnostics — look
for the lightbulb on any `RandomSkunk.StructuredLogging` logging call, not just on visible
warnings.

| Fix | Anchor diagnostic | What it does |
| --- | --- | --- |
| Use the equivalent `RandomSkunk.StructuredLogging` extension method | `RSSL0001` | Converts a `Microsoft.Extensions.Logging` `Log`/`LogTrace`/`LogDebug`/`LogInformation`/`LogWarning`/`LogError`/`LogCritical` call to the equivalent `RandomSkunk.StructuredLogging` call, turning each `{PropertyName}` message-template placeholder into a `<PropertyName>` tag on the corresponding interpolation hole. |
| Move `'PropertyName'` to a structured property argument | `RSSL0002` | Takes an interpolation hole already using the `<PropertyName>` tag format and moves it out of the message text into a trailing `("PropertyName", value)` tuple argument, leaving the value's default formatting in the message (or removing it from the message entirely if it wasn't otherwise referenced). |
| Remove the `'PropertyName'` tag format, keeping it in the message only | `RSSL0002` | The inverse: strips the `<PropertyName>` tag from an interpolation hole so the value stays in the message text but is no longer captured as a structured property. |
| Capture as a structured property named `'PropertyName'` / Capture as a destructured structured property named `'PropertyName'` | `RSSL0003` | Adds a `<PropertyName>` (or destructuring `<@PropertyName>`) tag to an interpolation hole that isn't currently capturing a structured property, guessing the property name from the hole's expression (e.g. `{user.Id}` → `<Id>`). |
| Move `'PropertyName'` into the message | `RSSL0004` | Takes a trailing `("PropertyName", value)` tuple argument and inlines it into the message as a `{value:<PropertyName>}` interpolation hole, removing the separate tuple argument. |
| Use the equivalent `Microsoft.Extensions.Logging` extension method | `RSSL0005` | The inverse of the `RSSL0001` fix: converts a `RandomSkunk.StructuredLogging` call back to the equivalent `Microsoft.Extensions.Logging` call, described in the `RSSL0005` row above. |
| Add a `using` declaration | `RSSL0006` | Turns the flagged statement into a `using` declaration — adds the `using` keyword to an existing local declaration, or introduces one (named `log`/`subLog`, or a disambiguated variant if that name's already in use) for a bare or discarded call. Offered only when the call is the entire statement or the initializer of a single-variable declaration; a call nested inside a larger expression (e.g. passed as an argument) is left for the developer to fix by hand. |
| Add a `using` block | `RSSL0006` | The block form of the same fix: wraps the flagged statement in a `using (...) { }` block with an empty body, rather than guessing which surrounding statements the developer meant to move into it — they're expected to do that by hand. Offered under the same conditions as the `using` declaration fix. |

## License

[MIT](https://github.com/bfriesen/RandomSkunk.StructuredLogging/blob/main/LICENSE)
