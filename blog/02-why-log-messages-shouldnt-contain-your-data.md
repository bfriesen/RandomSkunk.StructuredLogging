# Why Your Log Messages Shouldn't Contain Your Data

In the [last post](01-introducing-randomskunk-structuredlogging.md) we introduced
RandomSkunk.StructuredLogging and its basic pitch: messages and structured data are two different
concerns, so stop writing them as one string. This post makes the case for *why* that separation
matters, independent of which library you use to get there.

## The template is doing two jobs at once

Here's a completely ordinary log call:

```csharp
logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

That one string is being asked to do two unrelated jobs simultaneously:

1. **Be a message a human reads** in a console, a log viewer, an alert. It needs to read naturally,
   in whatever order and phrasing makes sense to a person skimming it at 2am.
2. **Be a template that names structured fields**, in the exact order the positional arguments are
   passed, using names that become the keys other systems (Seq, Elasticsearch, Application
   Insights, whatever) index and query on.

Those two jobs pull in different directions constantly. A message that reads well
("User 42 logged in from 10.0.0.1 after 3 failed attempts") doesn't necessarily want its fields in
the order a query planner wants them. A field that's genuinely useful to capture — a correlation
ID, a tenant ID, a feature-flag state — often has *no* natural place in a readable sentence at all.
The template format has no way to attach a property without also inserting it into the message
text somewhere.

## The failure modes this produces

Once you've stared at enough production logging code, a few patterns show up over and over:

**Fields crammed in for no reason but capture.** You'll see messages like:

```csharp
logger.LogInformation("Processing order {OrderId} {CorrelationId} {TenantId}", orderId, correlationId, tenantId);
```

which reads as "Processing order 8842 f3a1-9c2b… acme-corp" — nobody wanted `CorrelationId` or
`TenantId` in the sentence. They're there because the template is the only channel that exists for
attaching structured data.

**Positional argument mismatches.** `LogInformation`'s parameters are `params object?[]`, matched
to `{PlaceholderName}` tokens by *position*, not by name. Reorder the arguments, add one in the
middle, or edit the template text without touching the call site (or vice versa), and the compiler
won't catch the mismatch — you'll find out when a dashboard shows `UserId = "10.0.0.1"`.

**Message templates as unintentional cache keys.** Structured logging providers built on top of
`ILogger` cache a formatter per distinct template string, because re-parsing the template on every
call would be wasteful. That's a reasonable optimization — until a message template is built with
string interpolation or concatenation instead of held constant, and the "cache" quietly becomes an
unbounded dictionary keyed by every distinct message ever produced.

**Fighting the format to get readable output.** Want a timestamp formatted as `HH:mm:ss` in the
message but need the raw `DateTime` captured for querying? The template format can't do both from
one placeholder — you either format it for the message and lose precision in the captured value, or
capture the raw value and let it render however `ToString()` says.

## What "separate but explicit" gets you

The fix isn't complicated: stop making the message string double as the property list. Write the
message as an actual message, worded however reads best, and attach properties next to it,
explicitly, by name:

```csharp
logger.Information($"User logged in", ("UserId", userId), ("IpAddress", ipAddress));
```

Nothing here is inferred from string parsing or positional matching. `UserId` is a compile-time
tuple element name bound directly to `userId`'s value — there's no template to keep in sync, and no
way for the property name and the value to drift apart.

And when a value genuinely belongs in *both* places — the message and the structured properties —
that's still one hole, not two mechanisms fighting each other:

```csharp
logger.Debug($"[{ts:<Timestamp>HH:mm:ss}] tick");
// message text uses "HH:mm:ss" to format ts; the Timestamp property holds the raw DateTime
```

The message gets the human-friendly formatting; the structured property gets the unformatted
value. One hole, two outputs, no compromise between them.

## This isn't a RandomSkunk-specific idea

Serilog's message templates already improved on plain string formatting by making placeholders
named instead of positional. What RandomSkunk.StructuredLogging adds on top is refusing to require
a property to also live in the message text at all — the message is just a message, and every
property is attached explicitly, whether or not it appears in the sentence. If you're on Serilog,
NLog, or straight `Microsoft.Extensions.Logging`, the underlying argument here — *decide what a
human needs to read and what a machine needs to query as two separate decisions* — applies
regardless of which library ends up enforcing it for you.

## Next up

Post 3 goes deep on the `<PropertyName>`/`<@PropertyName>` tag format shown above — every mode it
supports, how destructuring works, and the edge cases (empty tags, malformed tags) worth knowing
about.
