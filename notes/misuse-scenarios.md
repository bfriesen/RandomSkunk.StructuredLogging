# Misuse scenarios and surprising behaviors

Working notes on ways a developer could misuse this library, or be surprised by
what it does, based on a review of the interpolated-string handlers, tag-format
parsing, destructuring, property capture, and operation logging. Intended as
input for future work (README "Common Pitfalls" section, analyzer ideas,
possible guard rails in the implementation).

## 1. Silently defeating the "don't evaluate when disabled" optimization

The perf trick only fires when the message argument is *statically* an
interpolated-string-literal bound to the handler overload. Anything that
forces the argument through the plain `string` overload instead makes C#
build the string eagerly, regardless of level, with none of the tag parsing:

- `logger.Debug($"...".ToUpper())` or any method call/`+`-concat applied to
  the interpolated string
- Assigning to a `string` local first: `string msg = $"User {name}";
  logger.Debug(msg);`
- A ternary mixing an interpolated string with a plain string often infers
  `string`, not the handler type
- Passing the interpolated string through a wrapper/helper method that isn't
  itself written with the `[InterpolatedStringHandler]` + `logger` pattern

None of these fail to compile or throw — they just quietly become as
expensive as pre-library `ILogger` calls, defeating the reason someone chose
this library.

**Decision:** Document in README "Common Pitfalls". Also try building an
analyzer that flags the common defeating patterns (a method call/operator
applied directly to an interpolated-string-literal argument of one of our
logger methods, e.g. `logger.Debug($"...".ToUpper())`) as a warning.

**Status:** Done. Documentation added as a top-level "Common pitfalls" section
in the README (after "Migrating from `Microsoft.Extensions.Logging`", before
"Operation logging") covering the method-call/concatenation form, the
local-variable form, and the wrapper/helper-method form. Analyzer for the
method-call/operator form shipped as
`InterpolatedStringMessageExpressionAnalyzer`/`RSSL0009` (warning): flags a
RandomSkunk.StructuredLogging call whose `message` argument applies a method
call or `+` concatenation directly to an interpolated string literal (e.g.
`logger.Debug($"...".ToUpper())`, `logger.Debug($"..." + suffix)`, including
chained calls like `.ToUpper().Trim()`). Deliberately does *not* flag the
case where the literal is behind a local variable first (that's `RSSL0007`'s
concern). No code fix - like `RSSL0007`, there's no single mechanical
rewrite that's always correct. Documented in the README's analyzers table
and cross-referenced from "Common pitfalls".

Analyzer for the wrapper/helper-method form (third bullet) shipped as
`InterpolatedStringHelperMethodArgumentAnalyzer`/`RSSL0010` (warning): flags
a RandomSkunk.StructuredLogging call whose `message` argument is a call to
some other method that was itself handed an interpolated string literal as
one of its arguments (e.g. `logger.Debug(FormatMessage($"..."))`), including
through a chain of nested helper calls (`FormatMessage(FormatMessage($"..."))`).
Deliberately does *not* flag the literal being behind a local variable first
(`RSSL0007`'s concern) or a method called directly on the literal
(`RSSL0009`'s concern) - those are separate patterns with separate
diagnostics. No code fix, same reasoning as `RSSL0007`/`RSSL0009`.
Documented in the README's analyzers table and cross-referenced from
"Common pitfalls".

**Scope narrowed:** `RSSL0007`/`RSSL0009`/`RSSL0010` were later scoped down
to only fire when the interpolated string literal actually captures a
`<PropertyName>` tag (e.g. `$"{amount:<Amount>}"`). A tagless interpolated
string that loses the interpolated-string-handler overload only loses the
disabled-level evaluation optimization - a real but lower-severity concern
these three diagnostics don't police, to keep them from firing on every
plain `.ToUpper()`/helper-method/local-variable pattern in a codebase.

## 2. `string msg = $"...{x:<Name>}..."` is actively dangerous, not just inert — ✅ Complete

If a developer assigns the interpolated string to a `string` *before* passing
it to the logger, the compiler uses the ordinary interpolation handler, not
this library's. `<Name>` (or `<@Name>`) is then handed to `x`'s real
`IFormattable.ToString(format)` as a genuine .NET format string. Most types
don't understand `"<Name>"` as a format — this throws `FormatException` at
the log call site in production, and only when that code path actually runs
(e.g., someone enables Debug logging to troubleshoot an issue and the app
starts throwing).

**Decision:** Document prominently as the top pitfall. Also addresses the
second bullet of #1 (`string msg = $"User {name}"; logger.Debug(msg);`),
since both are the same underlying pattern — assigning an interpolated
string to a local before passing it to the logger.

**Status:** Done. `InterpolatedStringLocalMessageAnalyzer` ships as
`RSSL0007` (warning): flags a call to a RandomSkunk.StructuredLogging
Trace/Debug/Information/Warning/Error/Critical/Write method whose `message`
argument is a local variable declared with an interpolated string
initializer. Documented in the README's analyzers table.

## 3. A missing `>` in a tag is a silent runtime format bug, not a compile error — ✅ Complete (fix)

`{value:<UserId}` (typo, no closing `>`) parses to `TagFormat(null,
"<UserId")` — capture is silently skipped, and the *entire* literal
`"<UserId"` becomes the real format string passed to `value`'s
`IFormattable`. Same failure mode as #2: works fine until the log level is
enabled, then throws or renders garbage. Nothing about this is flagged at
compile time.

**Decision:** Fix. Since the `<>` escape exists specifically for "I want a
real format starting with `<`," any other unterminated `<...` tag is almost
certainly a typo. Change `LogPropertyTagFormat.ParseCore` to throw a clear
`FormatException` at the point of use instead of silently passing the raw
text through as a format string. Also try building an analyzer that flags an
unterminated `<...` tag literal as a warning at compile time, since format
specifiers in interpolated holes are always compile-time constants.

**Status:** Fix done. `LogPropertyTagFormat.ParseCore` now throws a new
`UnterminatedLogPropertyTagException` (a `FormatException` subclass) when a
tag starting with `<` has no matching `>`, instead of silently treating the
raw text as a real format string. The exception carries a message pointing
at the `<>` escape hatch as the fix. Thrown at the point of use (inside
`AppendFormatted<T>`), so it only fires when the interpolation hole is
actually appended (i.e., the log level is enabled) — consistent with the
rest of the tag-parsing behavior. Covered by
`PropertyTagCaptureTests.UnterminatedTag_ThrowsFormatException` and
`UnterminatedTag_Disabled_DoesNotThrow`.

Analyzer also done: `UnterminatedLogPropertyTagAnalyzer` ships as `RSSL0008`
(Error, unlike every other analyzer in this package which is Warning/Info/
Hidden — an unterminated tag is unconditionally a bug, not a style
suggestion). It flags an interpolation hole whose format specifier starts
with `<` but never closes with `>`, scoped to holes that convert to one of
the library's interpolated-string-handler types (mirroring
`LogPropertyTagFormatAnalyzer`'s RSSL0002 scoping). No code fix was
requested/added - the diagnostic exists to surface the typo at compile time,
not to auto-correct it (there's no single right fix: it could be a missing
`>`, a missing property name, or the developer meant the `<>` escape).
Documented in the README's analyzers table.

## 4. Duplicate property names are never deduplicated, anywhere

Tag-captured properties, tuple-arg properties, and a caller-supplied
`IReadOnlyCollection` are just concatenated (`ConcatPropertyList`/
`LogPropertiesState`) with zero collision checking. Writing the same property
name via a tag *and* a tuple argument in the same call silently produces two
entries with the same key — behavior at the sink (Serilog, MEL console, etc.)
is provider-defined and often surprising (duplicate/overwritten/ambiguous
fields).

**Decision:** Document. Future analyzer idea: flag the same literal property
name used twice within one call (mostly compile-time detectable, since tag
names and tuple-arg names are almost always literals).

## 5. `<>` vs `<@>` vs a real format that starts with `<`

- `<>` is documented as "opt out of capture, strip the tag" — easy to misread
  as "use default format."
  - `<>` is only actually useful when *both* conditions hold: the developer
    doesn't want to capture the interpolation hole, *and* the real format
    string they want happens to start with `<`. If they don't want to
    capture and their real format starts with anything other than `<`, they
    can just write that format directly with no `<>` prefix — a plain
    (untagged) format specifier already means "no capture." Similarly, `<@>`
    is only useful when the developer doesn't want to capture the hole but
    *does* want it rendered into the message using Serilog-style
    destructured formatting instead of `IFormattable`/`ToString()`; if
    neither capture nor destructured rendering is wanted, no tag is needed
    at all.
- Only ever *one* tag is parsed per hole — whatever comes after that tag's
  closing `>` is handed straight through as literal format text, never
  re-parsed for a second tag. This cuts both ways:
  - It's what lets a real format that itself starts with `<` (or even `<>`)
    survive uncorrupted after a normal `<PropertyName>` tag, with no escaping
    needed: `{amount:<Amount><>myformat}` captures `Amount` *and* formats with
    the literal text `<>myformat`, because everything after `<Amount>`'s
    closing `>` is taken verbatim. Same for the no-capture case:
    `{x:<><>myformat}` opts out of capture and formats with `<>myformat`. If
    the parser instead looped to strip every leading `<>` it found, both of
    these legitimate "format literally starts with `<>`" cases would break.
  - But it also means a developer who has learned the `<>` escape hatch and
    reaches for it *and* wants to capture a property will naturally try
    stacking a second tag onto the escaped remainder: `{value:<><Amount>F2}`,
    expecting `<>` to strip and `<Amount>` to still be parsed as a capture
    tag. It isn't — `<>` already consumed the one tag slot, so `<Amount>F2`
    is treated as literal format text passed straight to `double.ToString()`.
    That's not a valid custom numeric format string, but .NET doesn't throw
    for that — it echoes the unrecognized characters back verbatim, so the
    message silently renders as `<Amount>F2` instead of the formatted number,
    and no property is captured at all. See
    `PropertyTagCaptureTests.EmptyTagEscapeHatch_CannotAlsoCaptureAProperty`
    for a reproduction. The fix for a developer in this situation is simply
    to put the property name in the *first* (and only) tag —
    `{value:<Amount>F2}` — rather than stacking a second one.
- A stray space breaks destructuring detection silently: `{x:< @Foo>}` is
  *not* recognized as destructure-mode (the `@` check is position-exact at
  index 1) — instead `" @Foo"` (with leading space) becomes the literal
  property name.
- Format text after a `<@...>` tag is silently discarded — `{amount:<@Amount>C}`
  does not apply currency formatting; no warning.

**Decision:** Document. Also consider an analyzer that flags format text
following a `<@...>` tag (since it's always silently discarded) as a warning —
worth a quick feasibility look, lower priority than #1/#3/#4 since it's a
narrower case.

## 6. Destructuring captures the raw value, not the rendered text

The captured property value is always the raw, undestructured value. If the
log sink is async/batched (common with Serilog sinks), and the object is
mutated after the log call returns but before the sink serializes it, the
structured property can disagree with what the message text showed at call
time — a TOCTOU trap for anyone logging mutable objects.

**Decision:** Document as an intentional tradeoff (capturing pre-rendered
text would mean always stringifying eagerly).

## 7. Destructuring only sees public instance properties, never fields

A struct or DTO built around public fields destructures to an empty `{ }` —
silent data loss, no exception, no hint anything was skipped. Also silently
truncates at `MaxDepth=10`/`MaxCollectionItems=10` (`...`) and swallows
throwing property getters into `<getter threw X>` text forever, which can
mask a real bug in that getter indefinitely.

**Decision:** Fix (planned, not urgent). Extend
`LogPropertyDestructuring.AppendObject` to also include public instance
fields. Tracked as a backlog item.

## 8. Tag-based capture always boxes, even for value types

The generic `AppendFormatted<T>(T value, string? format)` boxes into
`List<KeyValuePair<string, object?>>`. The whole reason the arity-1–6 tuple
overloads exist is to avoid boxing — but a developer who chooses `{x:<X>}`
tag syntax instead of a tuple arg loses that benefit without any signal that
they did.

**Decision:** Document as a known tradeoff; not fixable without doubling the
generated surface area.

## 9. Operation logging: forgetting `using` silently drops the entire journal — ✅ Complete

`BeginOperation` returns an `IDisposable`; nothing enforces disposal. Forget
it, and no log entry is ever written — not even a partial one — and the
pooled `StringBuilder` never returns to `OperationLogPools`, quietly starving
the pool for other operations.

**Decision:** Document + analyzer. Verified experimentally (scratch project
referencing the `0.10.1-alpha04` build with `AnalysisMode=All` and
`dotnet_diagnostic.CA2000.severity=warning` forced on) that **CA2000 does
not catch this**: its escape analysis anchors on `new`-expressions visible
in the consuming compilation, and `BeginOperation`'s internal `new
RootOperationLog(...)`/`new DisabledOperationLog(...)` live inside the
already-compiled library DLL, opaque to CA2000. A `new StreamReader(...)`
left undisposed in the same test project *did* get flagged, confirming
CA2000 works generally but simply can't see through a factory method from a
referenced assembly. Since consumers only ever obtain an `IOperationLog` via
`BeginOperation`/`BeginSubOperation` (never `new`), CA2000 will essentially
never fire here in practice — this needs our own analyzer (flag a local
typed `IOperationLog`/assigned from `BeginOperation`/`BeginSubOperation`
that isn't disposed on all paths) rather than relying on CA2000.

**Status:** Done. `UndisposedOperationLogAnalyzer` ships as `RSSL0006`
(warning), with two code fixes (add a `using` declaration / add an empty
`using` block) and correct handling of `IOperationLog`'s fluent
`AddProperty`/`Append`/`AppendValue`/`AppendJson`/`SetException`/`SetResult`
chain, since those all return the same instance that needs disposing.
Documented in the README's new "Common pitfalls" subsection under
"Operation logging".

## 10. Operation logging: using a sub-operation after the root is disposed corrupts an unrelated log entry — ✅ Complete

The nastiest one found. `RootOperationLog.DisposeCore()` returns the shared
journal `StringBuilder` to the pool. If any `ChildOperationLog` reference is
still alive (e.g., leaked out of scope, or a fire-and-forget task holding it)
and appends to it afterward, it may now be mutating a `StringBuilder` that's
been handed out to a completely different, unrelated operation elsewhere in
the app. The bug manifests as garbled text in some other log line, with no
exception and no connection back to the actual root cause.

**Decision:** Fix (planned). Add a disposed/generation guard to
`OperationLogState` so a `ChildOperationLog` used after the root's
`Dispose()` throws `ObjectDisposedException` instead of silently mutating a
pooled `StringBuilder` that's been handed to a different operation.

**Status:** Done. `OperationLogState` gained an `IsDisposed` flag, set the
moment the root returns the journal `StringBuilder` to `OperationLogPools`
(`ReturnJournalToPool`), plus a `ThrowIfDisposed()` helper. Every mutating
member on `OperationLog<TSelf>` (`AddProperty`, `Append`, `AppendValue`,
`AppendJson`, `BeginSubOperation`) and on `RootOperationLog`/
`ChildOperationLog` (`SetException`, `SetResult`, and `ChildOperationLog`'s
`DisposeCore`) now calls it first, so any leaked `ChildOperationLog` used or
disposed after the root has been disposed throws `ObjectDisposedException`
instead of touching a `StringBuilder` that may already belong to a different
operation. `IOperationLog`'s XML docs document the new exception. Covered by
`OperationLoggingTests.LeakedSubOperation_UsedAfterRootDisposed_ThrowsObjectDisposedException`
and `LeakedSubOperation_DisposedAfterRootDisposed_ThrowsObjectDisposedException`.

## 11. Not thread-safe by default, and it's easy to reach for concurrently — ✅ Complete

`IOperationLog` feels like the natural thing to close over in
`Task.WhenAll`/`Parallel.ForEach` sub-operations, but concurrent use without
`threadSafe: true` races on the shared `StringBuilder`/`List<Properties>` —
corruption or lost writes, no exception guaranteeing a loud failure.

**Decision:** Document only. This is an intentional opt-in design
(`threadSafe: true`), just needs to be prominent in the README.

**Status:** Done. Already documented in the README's dedicated "Thread
safety" subsection (under "Operation logging"), which predates this notes
file — added back when thread-safety was made opt-in
(`338ecef`/`93a5fa1`). States plainly that operation logs are "not
thread-safe by default," shows the `threadSafe: true` opt-in with a
`Task.WhenAll` sub-operation example, and explains the whole tree (root +
every nested sub-operation) shares one lock. No further action needed.

## 12. `SetException`/`SetResult` on a sub-operation don't do what they look like they do

Documented, but easy to miss: calling `subLog.SetException(ex)` does not set
the root log entry's actual `Exception` (the parameter APM/error-tracking
tools key off). It only appends the stack trace as journal text. A caught
exception logged this way is invisible to Sentry/Application Insights-style
exception grouping — only a human reading the journal body will ever see it.

**Decision:** Document, more prominently than the current XML doc comment —
this is the trap most likely to bite someone integrating with
exception-tracking tooling.

## 13. `Operation.*` property names are implicitly reserved

`RootOperationLog.DisposeCore` adds `"Operation.Result"` via the same
`AddProperty` list a caller can write to, and passes `"Operation.Name"`,
`"Operation.StartTime"`, `"Operation.DurationSeconds"` as trailing args. A
caller who calls `log.AddProperty("Operation.Name", ...)` gets a silent
duplicate-key collision with no warning that this prefix is off-limits.

**Decision:** Fix. Add a guard in `AddProperty` (root/child/disabled paths)
that throws `ArgumentException` if `propertyName` starts with `"Operation."`.

## 14. Level is frozen at `BeginOperation`, not re-checked at dispose

Unlike every other call in this library (which checks `IsEnabled` fresh, per
call), an operation's enabled/disabled state is decided once at
`BeginOperation` and never revisited. A long-running operation that spans a
dynamic log-level reload can silently log when it "shouldn't," or vice
versa — silently never log despite the level having flipped on mid-flight.

**Decision:** Document as intentional, consistent with `EventId` also being
fixed for the whole tree.

## 15. `Append`/`AppendValue`/`AppendJson`/`SetResult` arguments are always eagerly evaluated

Unlike the interpolated-string message builders, these take plain typed
parameters — C# has no way to defer evaluation of a method argument.
`expensiveComputation().RecordResultTo(log)` always pays for
`expensiveComputation()` even when the operation is disabled. This breaks the
mental model a developer will have built from the headline "logging that
skips work when disabled" feature.

**Decision:** Document as an inherent C# limitation (no handler trick is
possible for arbitrary typed parameters).

## 16. `BeginSubOperation` writes its "started" line unconditionally, even if the child is discarded

If a developer begins a sub-operation, then abandons it without appending or
disposing (e.g., an early-return before doing the work it was meant to
wrap), the journal permanently shows a `"started."` line with no matching
`"complete."` line — a silently half-finished-looking journal with no error
raised.

**Decision:** Document only. Matches canonical-log-line semantics; not
something to special-case.

## Rough priority

Two buckets stand out as most worth addressing first:

- **(a)** Anything that turns an interpolated-string call into a plain-`string`
  call defeats both the perf trick and the tag-capture feature, with the
  worst case being an actual runtime `FormatException` from a `<Tag>` leaking
  into `IFormattable.ToString` (#1, #2, #3).
- **(b)** Operation logging's shared, pooled, non-thread-safe state means
  sub-operation lifetime mistakes can corrupt logs from a totally different
  operation elsewhere in the app (#9, #10, #11).

## Decisions summary

| # | Scenario | Decision |
|---|----------|----------|
| 1 | Defeating the disabled-check optimization | ✅ Documented (README "Common pitfalls") + analyzers shipped as `RSSL0009`/`RSSL0010` |
| 2 | `string msg = $"...{x:<Name>}..."` assigned before logging | ✅ Document (top pitfall) + analyzer — shipped as `RSSL0007` |
| 3 | Missing `>` in a tag → runtime `FormatException` | ✅ Fixed: throws `UnterminatedLogPropertyTagException` (analyzer idea still outstanding) |
| 4 | Duplicate property names never deduped | Document (+ future analyzer idea) |
| 5 | `<>`/`<@>` edge cases | Document (+ consider analyzer for text after `<@...>`) |
| 6 | Destructuring captures raw (live) value | Document (by design) |
| 7 | Destructuring skips fields | Fix (backlog, not urgent) |
| 8 | Tag-based capture always boxes | Document (known tradeoff) |
| 9 | Forgetting `using` drops the journal | ✅ Document + analyzer (CA2000 confirmed *not* to catch this) — shipped as `RSSL0006` |
| 10 | Sub-operation used after root disposed corrupts unrelated log | ✅ Fixed: `ObjectDisposedException` guard |
| 11 | Not thread-safe by default | ✅ Document (intentional opt-in) — already covered by README's "Thread safety" subsection |
| 12 | Sub-operation `SetException`/`SetResult` don't touch root | Document (more prominently) |
| 13 | `Operation.*` reserved property names | Fix (throw `ArgumentException`) |
| 14 | Level frozen at `BeginOperation` | Document (intentional) |
| 15 | Operation-log args always eagerly evaluated | Document (C# limitation) |
| 16 | `BeginSubOperation` writes "started" unconditionally | Document (by design) |

Planned code changes: **#13** (fix); **#7** (backlog
enhancement). Analyzer ideas to investigate: **#1**, **#3** (fix shipped;
analyzer still outstanding), **#4** (stretch), **#5** (stretch, lower
priority). **#2** shipped as `RSSL0007` (also covers the second bullet of
#1). **#3** fix shipped as `UnterminatedLogPropertyTagException`. **#9**
shipped as `RSSL0006`. **#10** shipped as an `ObjectDisposedException` guard
in `OperationLogState`. **#11** confirmed already documented in the README's
"Thread safety" subsection.
