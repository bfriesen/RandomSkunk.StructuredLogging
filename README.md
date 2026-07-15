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

### 3. Dynamic properties, or more than 6 — `params` array

```csharp
logger.Information(
    $"Order processed",
    ("OrderId", orderId),
    ("Total", total),
    ("Currency", currency),
    ("Tax", tax),
    ("Discount", discount),
    ("Shipping", shipping),
    ("CouponCode", coupon));
```

Every generic-tuple call in option 1 is really calling one specific overload per arity; once a
call needs more than 6 properties, or the set of properties is built at runtime, this overload
takes over with the same call shape — just boxed `object?` values.

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

## Claude Code skill

The package ships a [Claude Code](https://claude.com/claude-code) skill file describing this
API. When you build a project that references `RandomSkunk.StructuredLogging`, the skill is
copied automatically to `.claude/skills/randomskunk-structuredlogging/SKILL.md`, so Claude Code
picks it up when writing or reviewing logging code in that project. Set
`RandomSkunkStructuredLoggingSkipSkillInstall` to `true` in your project to opt out.

## License

[MIT](LICENSE)
