# <img src="icon.png" alt="" width="32" height="32" valign="middle" /> RandomSkunk.StructuredLogging

[![NuGet](https://img.shields.io/nuget/v/RandomSkunk.StructuredLogging.svg)](https://www.nuget.org/packages/RandomSkunk.StructuredLogging)
[![CI](https://github.com/bfriesen/RandomSkunk.StructuredLogging/actions/workflows/ci.yml/badge.svg)](https://github.com/bfriesen/RandomSkunk.StructuredLogging/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

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

## Install

```bash
dotnet add package RandomSkunk.StructuredLogging
```

Targets `net8.0` and `net10.0`, and depends only on `Microsoft.Extensions.Logging.Abstractions`.

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

An empty tag (`<>`) strips itself out without capturing anything — use it when a real format
string happens to start with `<`: `{value:<>therealformat}`.

#### `<@PropertyName>` — Serilog-style destructured message text

A tag whose name starts with `@` renders the value into the *message text* using Serilog-style
destructured formatting instead of `ToString()`/`IFormattable` formatting, while still capturing
the raw, undestructured value as the structured property (destructuring never changes what gets
captured — only how it's rendered into the message):

```csharp
logger.Trace($"Item added to cart: {item:<@Item>}");
// message text:          "Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }"
// structured properties: Item = <the raw OrderItem instance>

logger.Trace($"Item added to cart: {item:<@>}");
// same message text, but the empty destructuring tag doesn't capture a structured property
```

Rendering rules: objects render as `TypeName { Prop1: Value1, Prop2: Value2 }` (anonymous types
omit the type name); collections render as `[item1, item2]`; dictionaries render as
`{ [key1]: value1, [key2]: value2 }`; strings and chars are quoted, other scalars (numbers,
`bool`, enums, `DateTime`, `Guid`, etc.) render unquoted using invariant culture; `null` renders as
`null`. Nested objects/collections are capped at 10 levels deep and 10 items per
collection/dictionary (both shown as `...` when exceeded), and a self-referencing object renders
`<circular reference>` instead of recursing forever. Any format text after a `<@...>` tag's
closing `>` is ignored, since destructured rendering fully replaces ordinary formatting.

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
        paymentLog.SetResult(receipt);
    }
    catch (Exception ex)
    {
        paymentLog.SetException(ex, recordEverywhere: true);
        throw;
    }
}

return order.RecordResultTo(log);
```

Disposing `log` writes a single log entry whose structured properties include `Operation.StartTime`,
`Operation.DurationSeconds`, `Operation.Result` (if set), any properties added via `AddProperty`, and an
`Operation.Journal` property holding the full journal - every `Append`/`AppendValue`/`AppendJson` call
and sub-operation start/result/failure/complete line, each timestamped with elapsed seconds. The
journal's header starts with the operation name; if `BeginOperation` was called with a non-default
`EventId`, the next header line shows its value; the header always ends with a `Start Time` line:

```
Operation: FulfillOrder
Start Time: 2026-08-19 12:56:31.417 -04:00
------------------------------------------
[0.002] Validating order 42
[0.003] `ChargePayment` started.
[0.041] `ChargePayment` result: Receipt { Id = ..., Amount = 99.00 }
[0.041] `ChargePayment` complete.
[0.042] Operation complete.
```

If the logger's level is disabled, `BeginOperation` returns an `IOperationLog` that never writes a
log entry and never accumulates a journal - no need to guard the call yourself. `EventId`,
`OperationName`, and `Properties` (via `AddProperty`) still behave normally even when disabled,
since code may read them regardless of whether the operation ends up logging anything.

### API at a glance

- `logger.BeginOperation(name)` / `logger.BeginOperation(eventId, name)` - both take optional
  `level` (default `LogLevel.Information`) and `threadSafe` (default `false`) parameters, and
  return an `IOperationLog`. The same interface represents both the root operation and every
  nested sub-operation - there's no separate sub-operation type.
- `IOperationLog.AddProperty<T>(name, value)` - adds a structured property to the final entry.
- `IOperationLog.Properties` - the properties added so far via `AddProperty`, as an
  `IReadOnlyList<KeyValuePair<string, object?>>`, readable while the operation is still open (the
  final entry isn't written until `Dispose`). The root and every sub-operation share the same
  list, since they all contribute to the one eventual entry. Under `threadSafe: true`, this
  returns a point-in-time snapshot rather than a live view, so it's safe to enumerate even while
  another thread is concurrently calling `AddProperty`.
- `IOperationLog.EventId` - the `EventId` the operation was begun with (via
  `BeginOperation(eventId, name, ...)`), or `default` if none was given. Same value on the root
  and every sub-operation. Useful for tagging a log line written elsewhere - e.g. from within the
  operation, or from code the operation called into - with the same `EventId` as the operation's
  own final entry, so the two can be correlated in a backend that indexes/filters by `EventId`.
- `IOperationLog.OperationName` - the `name` this operation was created with: `BeginOperation`'s
  `name` argument for the root, or `BeginSubOperation`'s `name` argument for a sub-operation.
  Unlike `EventId`/`Properties`, this is specific to each level - a sub-operation's
  `OperationName` is its own name, not its ancestor's.
- `IOperationLog.Append(text)` - appends a free-text line to the journal.
- `IOperationLog.AppendValue<T>(value, [valueName])` - appends `` `valueName`: value ``, where
  `valueName` defaults to the value expression's source text (via `CallerArgumentExpression`), so
  `log.AppendValue(order.Total)` appends `` `order.Total`: 42.50 `` with no name to spell out.
- `IOperationLog.AppendJson<T>(value, [valueName])` - same as `AppendValue`, but `value` is
  rendered as indented JSON instead of via `ToString()`/`IFormattable`.
- `IOperationLog.BeginSubOperation(name)` - starts a nested `IOperationLog`; disposing it
  appends a "complete" line to the parent's journal. Sub-operations never write their own log
  entry - only the root operation does, once, when *it's* disposed.
- `IOperationLog.SetException(exception, recordEverywhere = false)` - records the operation's
  exception. On the root, `recordEverywhere: true` additionally appends a "failed" line to the
  journal (the exception always becomes the final log entry's `Exception` regardless). On a
  sub-operation, a "failed" journal line is always appended, and `recordEverywhere: true`
  additionally sets it as the *root* operation's exception (the one that ends up on the final log
  entry).
- `IOperationLog.SetResult<T>(value)` - on the root, sets the `Operation.Result` structured
  property; on a sub-operation, appends a result line to the journal instead. Typically called via
  the fluent `value.RecordResultTo(log)` extension method so it can be chained directly onto
  a `return` expression.
- `value.RecordValueTo(log, [valueName])` / `value.RecordJsonTo(log, [valueName])` -
  fluent equivalents of `AppendValue`/`AppendJson` that return `value` unchanged, for chaining
  inline into an expression, e.g. `var total = order.Total.RecordValueTo(log);`.

Every method returns the same `IOperationLog`, so calls can be chained:
`log.AddProperty("OrderId", orderId).Append("Order validated");`.

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
| `RSSL0001` | Suggestion | Flags a call to one of `Microsoft.Extensions.Logging.LoggerExtensions`'s `Log`/`LogTrace`/`LogDebug`/`LogInformation`/`LogWarning`/`LogError`/`LogCritical` extension methods and suggests the equivalent `RandomSkunk.StructuredLogging` extension method — see [Migrating from `Microsoft.Extensions.Logging`](#migrating-from-microsoftextensionslogging) above. |
| `RSSL0002` | Silent | Marks an interpolation hole that uses the [`<PropertyName>` tag format](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `{who:<Recipient>}`, including the destructuring `<@PropertyName>` form) to capture a structured property. Silent by default — it exists to anchor code fixes that act on these holes, not to warn about anything. |
| `RSSL0003` | Silent | Marks an interpolation hole that does *not* use the [`<PropertyName>` tag format](#2-propertyname-format-tags--capture-a-value-thats-also-in-the-message) (e.g. `{who}`) to capture a structured property. Silent by default — it exists to anchor code fixes that act on these holes, not to warn about anything. |
| `RSSL0004` | Silent | Marks a name/value tuple argument (e.g. `("UserId", userId)`) passed at the end of a `RandomSkunk.StructuredLogging` extension method call to attach a structured property, when the name is a compile-time constant string (a literal, a constant concatenation, or an interpolated string whose holes are themselves constant strings). Silent by default — it exists to anchor code fixes that act on these arguments, not to warn about anything. |
| `RSSL0005` | Silent | Marks a call to any of the `RandomSkunk.StructuredLogging` `Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical`/`Write` extension methods, regardless of overload. Its code fix rewrites the call to the roughly equivalent `Microsoft.Extensions.Logging` call (the inverse of `RSSL0001`), moving each structured property into a `{PropertyName}` message-template placeholder — since a hole that isn't already tagged with a name (including a bare `<@>` destructuring tag) gets one guessed from its expression (the same guess RSSL0003's fix uses), most calls convert cleanly. A call is left unconverted only when a name truly can't be pinned down: a hole whose expression isn't a simple identifier, a tuple argument with a dynamically-computed name, or the leading collection-parameter overload. Silent by default — it exists to anchor code fixes that act on these calls, not to warn about anything. |

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

## License

[MIT](LICENSE)
