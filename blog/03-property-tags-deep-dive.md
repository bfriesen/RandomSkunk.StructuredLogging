# Property Tags: Capturing Structured Data Without Leaving the String

In [post 2](02-why-log-messages-shouldnt-contain-your-data.md) we argued that a log message and its
structured properties are two separate concerns, and that RandomSkunk.StructuredLogging keeps them
separate by attaching properties as explicit `(string Name, T Value)` tuples:

```csharp
logger.Information($"Order processed", ("OrderId", orderId), ("Total", total));
```

That covers the common case cleanly. But sometimes a value genuinely belongs in *both* places — the
reader wants to see it in the sentence, and a downstream system wants to query on it. Writing it
twice (`{orderId}` in the message text, `("OrderId", orderId)` as a tuple) works but invites the
same drift problem structured logging is supposed to fix: nothing keeps the two copies in sync.

The `<PropertyName>` tag format solves this at the interpolation hole itself.

## The basic tag

Any interpolation hole's format specifier can start with an "html-like" tag naming a structured
property:

```csharp
logger.Information($"User {userId:<UserId>} logged in from {ip:<IpAddress>}");
// message text:          "User 42 logged in from 10.0.0.1"
// structured properties:  UserId = 42 (int), IpAddress = <IPAddress instance>
```

One hole, two outputs. The message gets the value rendered as text (using whatever format follows
the tag, if any); the structured property gets the *raw*, unformatted value — not a re-parsed
string, the actual `int`/`IPAddress`/whatever it was.

That raw-value guarantee matters most with formatted values:

```csharp
logger.Debug($"[{ts:<Timestamp>HH:mm:ss}] tick");
// message text uses "HH:mm:ss" to format ts; the Timestamp property holds the raw DateTime
```

The message shows `14:32:07`; the `Timestamp` property holds the full `DateTime`, milliseconds and
all. Neither output compromises the other.

## When your format text starts with `<`

Because a leading `<` is how the parser recognizes a tag, a real format specifier that happens to
start with `<` needs an escape hatch: the empty tag, `<>`. It strips itself out and captures
nothing, leaving the rest of the format text to apply normally:

```csharp
{value:<>therealformat}
```

You only need this when your format text itself starts with `<`. If it doesn't, don't add `<>` —
`{value:therealformat}` already opts out of capturing on its own, since it doesn't start with `<`
at all.

## Malformed tags fail loudly, not silently

A tag that opens with `<` but never closes with `>` — usually a typo, like `{userId:<UserId}` —
isn't treated as a real format string handed to `IFormattable.ToString()`. It throws
`UnterminatedLogPropertyTagException` (a `FormatException`) at the point the hole is appended. The
alternative — silently trying to format `userId` with the literal string `"<UserId"` — would fail
in a much more confusing way, or in the worst case "succeed" with garbage output. The library's
analyzers package catches this exact mistake at compile time too (`RSSL0008`), so in practice
you'll rarely hit the runtime exception at all — more on the analyzers in a later post.

## `<@PropertyName>` — destructured rendering

A tag name starting with `@` does something more than capture a raw value: it also changes *how*
the value renders into the message text, from ordinary `IFormattable`/`ToString()` formatting to
Serilog-style destructured formatting.

```csharp
logger.Trace($"Item added to cart: {item:<@Item>}");
// message text:          "Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }"
// structured properties:  Item = <the raw OrderItem instance>
```

The captured structured property is unaffected — it's still the raw `OrderItem` instance, exactly
as if you'd used a plain `<Item>` tag. Destructuring only changes what ends up in the message text.
Any format specifier text after a `<@...>` tag's closing `>` is parsed but ignored, since
destructured rendering fully replaces ordinary formatting — there's no "destructure, but also
apply this format string" combination.

Destructured rendering follows a small set of rules:

- **Objects** render as `TypeName { Prop1: Value1, Prop2: Value2 }`. Anonymous types omit the type
  name.
- **Collections** render as `[item1, item2]`.
- **Dictionaries** render as `{ [key1]: value1, [key2]: value2 }`.
- **Strings and chars** are quoted; other scalars (numbers, `bool`, enums, `DateTime`, `Guid`,
  etc.) render unquoted, using invariant culture.
- **`null`** renders as `null`.
- Nesting is capped at 10 levels deep, and collections/dictionaries are capped at 10 items, both
  shown as `...` when exceeded — a large object graph won't blow up your log message.
- A self-referencing object renders `<circular reference>` instead of recursing forever.

Like the plain capture tag, `<@PropertyName>` has an empty form: `<@>`. It's for the case where you
want destructured *rendering* in the message but don't want the value captured as a structured
property at all:

```csharp
logger.Trace($"Item added to cart: {item:<@>}");
// same message text, but nothing is added to the structured properties
```

If you want neither destructured rendering nor capture, just skip the tag entirely.

## Why this is cheap

Format specifiers in interpolated strings are always compile-time literals — and compile-time
string literals are interned, meaning the exact same literal text is always the exact same `string`
instance at every call site that uses it, for the lifetime of the process. RandomSkunk.StructuredLogging
takes advantage of that: the first time a given literal tag is parsed, the `(PropertyName, Format)`
result is cached in a `ConcurrentDictionary` keyed by that literal reference. Every subsequent call
through that call site is a dictionary lookup on a fixed key, not a re-parse. And if a format
specifier doesn't start with `<` at all — the overwhelmingly common case for holes with no tag — the
parser returns immediately, before even touching the dictionary.

## Choosing between tuples and tags

Both mechanisms attach the same kind of structured property; the difference is purely about
whether the value also needs to appear in the message text:

- **Tuple argument** (`("Name", value)`) — the value isn't part of the sentence, or it fits better
  attached at the end of the call than woven into the message.
- **`<PropertyName>` tag** — the value is already going to appear in the message, and you don't
  want to write it (and keep it in sync) twice.
- **`<@PropertyName>` tag** — same, but you want the message to show a destructured, human-readable
  rendering of a complex object instead of relying on its `ToString()`.

## Next up

Post 4 looks at how the zero-cost `IsEnabled` check actually works — the `[InterpolatedStringHandler]`
mechanism behind every level method, and why a disabled `logger.Debug($"... {ExpensiveCall()}")`
truly never evaluates `ExpensiveCall()`.
