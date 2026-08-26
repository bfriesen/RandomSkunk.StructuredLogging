# Introducing RandomSkunk.StructuredLogging

Today we're launching **RandomSkunk.StructuredLogging**: a small library that changes how you call
into `Microsoft.Extensions.Logging`, without changing anything about your logging pipeline.

If you've written ASP.NET Core, worker services, or really any modern .NET app, you've written
code like this:

```csharp
logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
```

It's fine, right up until it isn't. Maybe the message needs to read differently than the
`{UserId}`/`{IpAddress}` template forces it to. Maybe you want to attach a property that has no
natural place in the sentence at all — a correlation ID, a feature flag, a duration. Maybe you just
want to write `$"User {userId} logged in"` like a normal interpolated string instead of memorizing
positional-argument order. `Microsoft.Extensions.Logging`'s `LogInformation`/`LogDebug`/`LogWarning`/
etc. extension methods force every structured property into the message template, and there's no
way around it.

RandomSkunk.StructuredLogging separates those two concerns. The message is a normal interpolated
string, worded however you want. Structured properties are attached separately and explicitly.

```csharp
using RandomSkunk.StructuredLogging;

logger.Information($"User logged in", ("UserId", userId), ("IpAddress", ipAddress));
```

Same `ILogger`, same log providers, same everything downstream — just a call site that doesn't
force your message and your data into the same string.

## Three things worth knowing right away

**It's not just tuples.** If a property is *also* something you want visible in the message text,
you can capture it right at the interpolation hole with a `<PropertyName>` tag instead of writing
it twice:

```csharp
logger.Information($"User {userId:<UserId>} logged in from {ip:<IpAddress>}");
// message text:          "User 42 logged in from 10.0.0.1"
// structured properties:  UserId = 42 (int), IpAddress = <IPAddress instance>
```

The message shows a formatted, human-friendly value; the structured property gets the real,
unformatted one. There's also a `<@PropertyName>` variant for Serilog-style destructured rendering
of objects, collections, and dictionaries — more on that in an upcoming post.

**It costs nothing when the level is disabled.** The message parameter is a custom
`[InterpolatedStringHandler]`, not a plain `string`. Its constructor checks `logger.IsEnabled(...)`
*before* any interpolation hole is evaluated, so `logger.Debug($"... {ExpensiveCall()}")` never
calls `ExpensiveCall()` when Debug is off. No `if (logger.IsEnabled(...))` guard required — you get
one for free.

**There's a second feature for a different problem.** Alongside these per-call log methods, the
library ships an **operation logging** feature: journal everything that happens during an operation
(and any nested sub-operations), then flush it as exactly *one* log entry when the operation
completes — a canonical-log-line / wide-event style of logging, instead of a scattered trail of
individual log lines. That's a big enough topic to get its own post later in this series.

## Getting it

```bash
dotnet add package RandomSkunk.StructuredLogging
```

It targets `net8.0` and `net10.0`, and depends only on `Microsoft.Extensions.Logging.Abstractions`
— nothing else in your logging pipeline needs to change.

There's also a companion analyzers package that flags existing `Microsoft.Extensions.Logging` calls
and offers a one-click fix to convert them. That's next up in this series.

## What's next

This is post 1 of a series introducing RandomSkunk.StructuredLogging. Coming up:

- Why log messages and structured data are two different concerns, in more depth
- A full tour of `<PropertyName>`/`<@PropertyName>` tags
- How the zero-cost `IsEnabled` check actually works under the hood
- Migrating an existing codebase with the analyzers package
- Canonical log lines in .NET: a deep dive into operation logging

If you want to try it now, the [README](../README.md) has the full API tour.
