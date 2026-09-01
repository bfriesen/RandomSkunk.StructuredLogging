# Misuse scenarios and surprising behaviors

Working notes on ways a developer could misuse this library, or be surprised by
what it does, based on a review of the interpolated-string handlers, tag-format
parsing, destructuring, property capture, and operation logging. Intended as
input for future work (README "Common Pitfalls" section, analyzer ideas,
possible guard rails in the implementation).

Items are never renumbered, and a scenario is deleted once it's resolved - hence
the gaps in the numbering. The summary at the end records which ones were removed
and what shipped in their place.

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
    and no property is captured at all. The fix for a developer in this
    situation is simply to put the property name in the *first* (and only)
    tag — `{value:<Amount>F2}` — rather than stacking a second one.
- A stray space breaks destructuring detection silently: `{x:< @Foo>}` is
  *not* recognized as destructure-mode (the `@` check is position-exact at
  index 1) — instead `" @Foo"` (with leading space) becomes the literal
  property name.
- `{amount:<@>F3}` doesn't read obviously as "no destructuring, format with
  F3" at a glance, even though that is exactly what it does — identical to the
  plain `<>F3` escape hatch, since there's no property name left for the `@`
  to attach to. `notes/planned-features.md` item #1 tracks adding an analyzer
  for that specific combination.

**Decision:** Document the remaining points above (the stray-space pitfall,
one-tag-per-hole).

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

**Decision:** Fix, but warn rather than throw. `Microsoft.Extensions.Logging`
itself does nothing about two ordinary duplicate property names (confirmed:
`logger.LogDebug("{User} {User}", a, b)` produces two `"User"` entries in the
state list, no exception, no dedup) - resolving duplicate keys is sink
policy, not something this library enforces elsewhere, and the four
`Operation.*` properties are passed to `Write` as trailing tuple arguments,
never through the same list `AddProperty` writes to, so a caller can't
actually overwrite them - only duplicate the key from the sink's point of
view. So `OperationLogBase.AddPropertyCore` (root/child paths; disabled has
no journal to warn into, which is fine since a disabled operation writes no
entry) appends a journal line - `"<name>" is a reserved property name; the
operation's own property of that name will be duplicated in the log
entry.` - when `propertyName` exactly matches one of the four names the
library actually writes (`Operation.Name`, `Operation.StartTime`,
`Operation.DurationSeconds`, `Operation.Result`), not just the `"Operation."`
prefix - an unrelated name like `Operation.Foo` doesn't collide with
anything the library writes, so warning about it would just be noise.

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
`expensiveComputation().SetResultTo(log)` always pays for
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

## Decisions summary

| # | Scenario | Decision |
|---|----------|----------|
| 4 | Duplicate property names never deduped | Document (+ future analyzer idea) |
| 5 | `<>`/`<@>` edge cases | Document (+ consider analyzer for `<@>` with a trailing format) |
| 6 | Destructuring captures raw (live) value | Document (by design) |
| 7 | Destructuring skips fields | Fix (backlog, not urgent) |
| 8 | Tag-based capture always boxes | Document (known tradeoff) |
| 12 | Sub-operation `SetException`/`SetResult` don't touch root | Document (more prominently) |
| 13 | `Operation.*` reserved property names | Fix (throw `ArgumentException`) |
| 14 | Level frozen at `BeginOperation` | Document (intentional) |
| 15 | Operation-log args always eagerly evaluated | Document (C# limitation) |
| 16 | `BeginSubOperation` writes "started" unconditionally | Document (by design) |

Planned code changes: **#13** (fix); **#7** (backlog enhancement). Analyzer
ideas to investigate: **#4** (stretch), **#5** (stretch, lower priority) - the
latter tracked in `notes/planned-features.md` item #1. Everything else here is
a documentation task.

Scenarios 1, 2, 3, 9, 10 and 11 were resolved and removed: 1/2/3 shipped as
`RSSL0009`/`RSSL0010`, `RSSL0007` and `RSSL0008` (plus
`UnterminatedLogPropertyTagException`) alongside the README's "Common
pitfalls" section, 9 as `RSSL0006`, 10 as the `ObjectDisposedException` guard
in `OperationLogState`, and 11 was already covered by the README's "Thread
safety" subsection. Their notes are in git history.
