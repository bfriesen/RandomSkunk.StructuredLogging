# Migrating from Microsoft.Extensions.Logging with One Click

The first four posts made the case for separating log messages from structured properties. None of
that matters much if adopting it means hand-editing every `LogInformation`/`LogDebug`/`LogWarning`
call in an existing codebase. It doesn't — RandomSkunk.StructuredLogging ships a companion
analyzers package that finds those calls and offers an automatic fix.

```bash
dotnet add package RandomSkunk.StructuredLogging.Analyzers
```

(It's also referenced automatically when you install the main package, so most projects won't need
to add it separately.)

## RSSL0001: the migration suggestion

`RSSL0001` flags any call to `Microsoft.Extensions.Logging.LoggerExtensions`'s `Log`/`LogTrace`/
`LogDebug`/`LogInformation`/`LogWarning`/`LogError`/`LogCritical` extension methods. It's an
Info/Suggestion-level diagnostic — a nudge, not a warning — that shows up as a lightbulb in your
IDE:

```csharp
logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);
//     ~~~~~~~~~~~~~~~ RSSL0001: Use the equivalent RandomSkunk.StructuredLogging extension method
```

Invoking the fix converts the call, turning each `{PropertyName}` message-template placeholder into
a `<PropertyName>` tag on the corresponding interpolation hole:

```csharp
logger.Information($"User {userId:<UserId>} logged in from {ipAddress:<IpAddress>}");
```

That's the conservative conversion — it preserves the original message wording exactly, just moving
the same properties into tags instead of a positional template. From there, it's an ordinary
manual edit (or a further `RSSL0002` fix — see below) to reword the message and pull a property out
into a trailing tuple argument instead, if you don't need it in the sentence:

```csharp
logger.Information($"User logged in", ("UserId", userId), ("IpAddress", ipAddress));
```

RSSL0001 gets you to working, equivalent code automatically. Whether to then reshape the message
text is a judgment call about what reads best — the analyzer doesn't try to guess that for you.

## Fine-tuning a converted call

Once a call is on `RandomSkunk.StructuredLogging`, four more (silent, by design) diagnostics anchor
additional code-fix-only actions — they don't produce warnings, they just make these fixes
available from the lightbulb menu at the relevant location:

- **`RSSL0002`** anchors on a hole already using a `<PropertyName>` tag, and offers two opposite
  fixes: move the property out into a trailing `("PropertyName", value)` tuple argument, or strip
  the tag entirely and leave the value in the message text only.
- **`RSSL0003`** anchors on a hole that *isn't* tagged, and offers to add a `<PropertyName>` (or
  `<@PropertyName>` destructuring) tag — guessing a starting name from the hole's expression (e.g.
  `{user.Id}` → `<Id>`), which you can then rename.
- **`RSSL0004`** anchors on a trailing `("PropertyName", value)` tuple argument with a
  compile-time-constant name, and offers to inline it into the message as a `{value:<PropertyName>}`
  hole instead.
- **`RSSL0005`** is the reverse of RSSL0001: it anchors on *any* `RandomSkunk.StructuredLogging`
  call and offers to convert it back to the equivalent `Microsoft.Extensions.Logging` call, useful
  if you need to hand code back to a team or project not using this library yet.

Together, these let you shape a call however you want after the initial mechanical conversion,
entirely through code fixes — reposition properties between the message and the argument list as
many times as you like without retyping the call by hand.

## The trap this library's analyzers also guard against

The zero-cost `IsEnabled` short-circuit from [post 4](04-zero-cost-logging-interpolated-handlers.md)
and `<PropertyName>` tag capture both depend on one thing: the compiler binding your `message`
argument to the `[InterpolatedStringHandler]` overload, which only happens when the argument is
*syntactically* an interpolated-string literal at the call site. A few innocent-looking refactors
break that binding silently:

```csharp
logger.Debug($"User {name}".ToUpper());          // method call on the literal forces `string`
logger.Debug($"count: " + count);                // string concatenation, same problem

string message = $"User {userId} logged in";     // interpolated string assigned to a local first
logger.Debug(message);                            // ...then passed as a plain `string`

logger.Debug(FormatMessage($"User {userId} logged in")); // passed through a wrapper/helper method
```

None of these fail to compile or behave visibly differently in the common case — they just silently
give up the disabled-level optimization. Worse: if any hole in one of these uses a `<PropertyName>`
tag, it's not just "no benefit," it's a latent bug. Once the string is built by the ordinary
compiler-provided handler instead of this library's, `<Name>` gets handed straight to
`IFormattable.ToString("<Name>")` as a real format string — which throws `FormatException` the
moment that code path actually runs (for example, the day someone flips on Debug logging in
production to chase down an issue, and the log statement itself starts throwing).

Three more analyzers exist purely to catch this class of mistake at compile time, before it ships:

- **`RSSL0007`** catches the local-variable form (`string message = $"..."; logger.Debug(message);`).
- **`RSSL0009`** catches the method-call/concatenation form (`.ToUpper()`, `+ suffix`).
- **`RSSL0010`** catches the wrapper/helper-method form (passing the literal into some other method
  that isn't itself written with an `[InterpolatedStringHandler]` parameter).

All three are Warning-severity, and all three only fire when a `<PropertyName>` tag is actually at
stake — a tagless interpolated string routed through one of these patterns only loses the
performance optimization, which none of these analyzers police, since it degrades silently rather
than throwing. The fix in every case is the same: pass the interpolated string literal directly as
the `message` argument, not through a variable, a method call, concatenation, or a helper.

## Bonus: `RSSL0006` catches a different kind of mistake

Not a migration analyzer, but worth knowing about if you're touching this code anyway: `RSSL0006`
flags a call to `BeginOperation`/`BeginSubOperation` (the operation-logging feature, covered
starting in post 6) whose returned `IOperationLog` isn't visibly disposed. Forgetting to dispose it
silently drops the entire journal — not even a partial log entry gets written — and ordinary
`CA2000` analysis can't catch it, since its escape analysis can't see through `BeginOperation`'s
internals from outside the already-compiled library assembly. Its code fix can add a `using`
declaration or wrap the call in a `using` block automatically.

## What migration actually looks like in practice

For most codebases, migrating is: run the RSSL0001 fix across a file or project (most IDEs support
"fix all in document/project/solution" for a single diagnostic), let the three eager-evaluation
analyzers (`RSSL0007`/`RSSL0009`/`RSSL0010`) flag anything the bulk fix couldn't safely handle, and
then selectively use `RSSL0002`–`RSSL0004` to reshape individual calls where the wording or property
placement should change. Nothing about the migration requires touching your logging pipeline,
providers, or configuration — only the call sites themselves change.

## Next up

Post 6 turns to the library's other major feature: operation logging, a canonical-log-line style of
logging that journals an entire operation and flushes it as exactly one log entry, instead of a
scattered trail of individual log lines.
