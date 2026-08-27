# Planned features

Working notes on features that are planned but not yet designed/implemented in
detail. Unlike `misuse-scenarios.md` (surprising/buggy existing behavior),
these are intentional additions to the API surface or behavior.

## 1. Apply trailing format text to the message after a destructuring tag, instead of discarding it

Currently, any format text following a `<@PropertyName>` (or `<@>`) tag is
parsed by `LogPropertyTagFormat.ParseCore` but silently ignored — see
`TagFormat.Format`'s doc comment ("Ignored when `Destructure` is `true`") and
misuse-scenarios.md #5's last bullet. For example:

```csharp
logger.Information($"Example: {amount:<@Amount>F3}");
// today: message text is the destructured rendering of `amount`; "F3" is discarded
```

**Planned change:** Stop discarding the trailing format. Instead:

- The **message text** should render `amount` using the trailing format
  (`"F3"`, via `IFormattable`/`ToString()`) — i.e., the message behaves as if
  the `@` weren't there and `<Amount>F3` were an ordinary capturing tag.
- The **captured structured property** should still be named so that a
  Serilog-style (or similar) sink recognizes it as a request to destructure
  the raw value at processing time. In Serilog's own convention this means
  the property name is prefixed with `@` (Serilog's own destructuring
  operator, distinct from this library's tag syntax) — i.e., the captured
  `KeyValuePair<string, object?>.Key` would be `"@Amount"` rather than
  `"Amount"`.

So `{amount:<@Amount>F3}` would mean: "format `amount` with `F3` in the
message text, but capture it as `@Amount` so a downstream processor
destructures it" — decoupling "how the value looks in the message" from "how
the sink should capture the structured value," which today are forced to be
the same choice (either both use `IFormattable`/plain, or both use
destructured rendering).

This is a meaningful behavior change from today's "trailing format after
`<@...>` is always ignored," so it needs a version bump / changelog entry and
README updates (the `<@PropertyName>` section, and the misuse-scenarios.md
entry needs its "Decision" revisited since the analyzer idea there — "flag
format text following a `<@...>` tag as a warning" — no longer applies once
that format text is meaningful).

### Open question: what about an *empty* destructuring tag with trailing format, e.g. `{amount:<@>F3}`?

`<@>` alone means "don't capture, but destructure the value into the message
text." If we start honoring a trailing format after `<@PropertyName>`, we
need to decide what `<@>F3` means, since there's no property name to prefix
with `@` — only the message-rendering half of the behavior is even in play.

Two options:

- **(Leaning toward this one) Be consistent with the non-destructuring
  `<>` escape hatch.** `<>F3` today means "no capture, use `F3` as the real
  format." By the same logic, `<@>F3` would mean "no capture, use `F3` as the
  real format" too — i.e., once there's no property name, the `@` no longer
  has anything to attach destructuring semantics to for capture purposes, and
  since we're now honoring trailing format for the named-property case, it
  would be inconsistent to keep ignoring it just because the tag is
  unnamed. Under this option, `<@>F3` and `<>F3` would behave identically
  (both: no capture, format the message with `F3`) — the `@` in `<@>` would
  then only matter when paired with a nonempty property name.
- **Ignore the trailing format for the empty-destructuring-tag case.** Keep
  today's "format after any `<@...>` tag is ignored" rule specifically for
  `<@>`, and only start honoring trailing format when the tag has a
  property name. This keeps `<@>` as an unconditional "destructure into the
  message, never anything else" escape hatch, at the cost of an inconsistency
  between `<@Name>F3` (format honored) and `<@>F3` (format ignored) that
  would need to be called out explicitly in docs.

**Status:** Undecided — leaning toward the first option (consistency with
`<>`), but not settled. Needs a decision before implementation.

**Regardless of which option is chosen, add an analyzer.** `{amount:<@>F3}`
is inherently ambiguous to a reader — it's not obvious at a glance whether the
trailing format is honored or discarded, since both an empty-property-name
tag and destructuring are stacked together. Whichever behavior we pick, the
combination of "empty destructuring tag" + "trailing format" should be
flagged (warning severity — this isn't a runtime bug like RSSL0008, just a
confusing construct with a strictly clearer equivalent) so the developer
simplifies to the format they actually want:

- If they want the format honored and no destructuring: write `{amount:<>F3}`
  (or, once option 1 above ships, `{amount:<@>F3}` would already do this —
  but see below).
- If they want destructuring and no format: write `{amount:<@>}` with the
  format text removed entirely.

A code fix could offer both choices explicitly (one fix arm rewriting to
`<>F3`-equivalent-with-destructuring-dropped, the other to `<@>` with the
format text deleted), letting the developer pick which behavior they meant
rather than requiring them to remember which one the ambiguous form silently
resolves to. This analyzer is worth adding independent of which option for
the open question above is chosen, since the ambiguity/confusion exists
either way — it just makes the "simplify instead of relying on it" case even
stronger once trailing-format-after-`<@Name>` becomes meaningful elsewhere.

### Implementation notes (once decided)

- `TagFormat.Format` is currently documented as "Ignored when `Destructure`
  is `true`" — that doc comment and the callers that currently skip using
  `Format` when `Destructure` is true (the `AppendFormatted<T>` overloads in
  the generated interpolated-string-handler structs) need updating.
- Need to decide the exact captured property name shape: is it always
  `"@" + PropertyName"`, or should the library store `PropertyName` unprefixed
  plus a separate `Destructure` flag on the captured entry (closer to how
  `TagFormat` itself models it internally) and let the *sink integration*
  decide whether/how to signal destructuring? Prefixing with `@` bakes in a
  Serilog-specific convention; storing it unprefixed is more sink-agnostic
  but requires each `ILogger`/sink pairing to know to look for a
  library-specific "destructure this" signal, which today doesn't exist for
  captured properties (`AddProperty`/tag-capture currently just deposit a
  plain `object?` value).
- Existing tests that assert trailing format after `<@...>` is ignored (if
  any — check `PropertyTagCaptureTests`/`PropertyTagDestructuringTests`) will
  need updating to assert the new behavior instead.

## 2. Analyzer: flag an unnecessary `<>` no-capture escape hatch

The `<>` tag exists for exactly one reason: to let a real format string that
itself starts with `<` coexist with the no-capture case, e.g.
`{value:<>therealformat}` (see README's "`<PropertyName>` format tags"
section and misuse-scenarios.md #5). It's a no-op everywhere else:

- `{value:<>}` — no format at all after the tag. This is identical to just
  writing `{value}` with no format specifier.
- `{value:<>F3}` — a trailing format that does *not* itself start with `<`.
  This is identical to just writing `{value:F3}` directly; the `<>` prefix
  contributes nothing since there's no leading `<` in the real format for it
  to disambiguate.

**Planned change:** Add an analyzer that flags a `<>` tag when the format
text following it is empty or does not start with `<` — the `<>` is
unnecessary in both cases, since a plain (untagged) format specifier already
means "no capture." Severity: warning (this isn't a bug, just noise/an
easy-to-copy-paste-and-forget-to-simplify pattern — someone may reach for
`<>` out of habit after using it correctly elsewhere, even when their
specific format doesn't need it).

**Code fix:** Remove the `<>` prefix, leaving the remaining format text (if
any) as a plain format specifier — `{value:<>}` → `{value}`, `{value:<>F3}` →
`{value:F3}`.

This only applies to the plain (non-destructuring) `<>` tag. It's unrelated
to the `<@>` empty-destructuring tag from #1 above, which always has a
purpose (destructured message rendering) independent of what a trailing
format is doing — #1's analyzer targets a different confusing combination
(`<@>` *with* a trailing format), while this one targets `<>` *without* a
reason to exist at all.

## 3. Interpolated string handler overloads for `IOperationLog.Append` and `BeginSubOperation`

**Proposed:** Add overloads of `Append` and `BeginSubOperation` whose
message/name parameter is a custom `[InterpolatedStringHandler]` struct
(instead of `string`), backed by `System.Text.StringBuilder`'s built-in
`AppendInterpolatedStringHandler` targeting the operation's own journal
`StringBuilder` — i.e. the interpolated content is written directly into the
journal instead of being built as a separate `string` first and then copied
in via `Append(string)`.

**Inferred rationale:** today, `log.Append($"Order {orderId} rejected: {reason}")`
first builds a throwaway `string` via the compiler's ordinary (default)
interpolated-string handling, which `OperationLog<TSelf>.Append` (`Operation/OperationLog.cs:42-47`)
then copies into the journal via `StringBuilder.Append(text)`
(`Operation/OperationLogState.cs:86-90`, `BeginJournalEntry()`). Since the
whole point of an `Append` call's argument is to become journal text and
nothing else, writing directly into the journal `StringBuilder` would skip
that intermediate allocation/copy — the same "avoid the extra step" instinct
behind the rest of this library's interpolated-handler design (see
`LogInterpolatedStringHandlers.g.cs`).

**Feasibility analysis — `Append`:** workable, but only with a new internal
seam, and it needs to interact correctly with the two existing decorator/wrapper
`IOperationLog` implementations:

- To have the handler write into the journal, its constructor needs a
  reference to that `StringBuilder` *at the call site*, before `Append`'s
  method body runs — via `[InterpolatedStringHandlerArgument("")]` binding to
  the receiver. Since most call sites hold an `IOperationLog` (the public
  interface), the handler constructor's parameter has to be typed
  `IOperationLog`, but `IOperationLog` deliberately never exposes the
  internal `OperationLogState`/journal (see "Operation logging" in
  CLAUDE.md). This needs a new internal-only escape hatch — e.g. an internal
  interface (`IJournalTarget { StringBuilder BeginJournalEntry(); }`)
  implemented explicitly by `RootOperationLog`/`ChildOperationLog`, which the
  handler pattern-matches for.
- **`DisabledOperationLog` — a genuine win.** The handler can expose an
  `IsEnabled`-style flag (`false` for a `DisabledOperationLog`, mirroring
  `handlerIsValid` on the level handlers), letting the compiler skip
  evaluating the interpolation holes entirely — matching
  `DisabledOperationLog`'s existing "zero journal accumulation" design intent
  (`Operation/DisabledOperationLog.cs:6-9`) better than today's plain
  `Append(string)`, which still pays for building the string before the
  no-op `Append` discards it.
- **`SynchronizedOperationLog` — resolved via a private rented buffer,
  spliced under lock.** An earlier version of this design had the handler's
  constructor itself take `gate` (via `Monitor.Enter`, bound to the receiver
  through `[InterpolatedStringHandlerArgument("")]`) and release it from a
  `ReleaseLock()` method called out of `Append`'s body. That doesn't work:
  the constructor and the `AppendFormatted` calls for each interpolation
  hole run as part of *argument evaluation*, before `Append`'s body is ever
  entered - if any hole's expression throws, `Append` (and any `finally` in
  it) never runs, so the lock is never released. Since every operation in a
  `threadSafe: true` tree shares one `gate`
  (`Operation/SynchronizedOperationLog.cs:11-14`), that's a permanent
  deadlock for the whole tree from then on, on any thread - a categorically
  worse failure than a soft resource leak (compare: if a hole's `ToString()`
  throws in one of the *level* handlers, the handler's pooled `char[]` just
  never makes it back to `ArrayPool<char>.Shared` -
  `eng/GenerateSource.cs:488-489` - a bounded, self-healing loss, not a hang).

  The design that avoids this: decide the write *target* based on the
  receiver's type, not on locking during evaluation.
  - Add a non-generic internal interface, e.g.
    `internal interface IJournalOwner { StringBuilder BeginJournalEntry(); }`,
    implemented by `OperationLog<TSelf>` (so both `RootOperationLog` and
    `ChildOperationLog` get it via the base they already share) by
    delegating to `_state.BeginJournalEntry()`.
  - In the handler's constructor (`log` bound via
    `[InterpolatedStringHandlerArgument("")]`): `handlerIsValid = log.IsEnabled`
    (item #6 - no type check needed for this part). If enabled and
    `log is IJournalOwner owner`, target `owner.BeginJournalEntry()`
    directly - no lock, same as today's unsynchronized fast path. Otherwise
    (in practice, `SynchronizedOperationLog`, but this also degrades safely
    for any future/foreign `IOperationLog` implementation that isn't
    `IJournalOwner`) rent a `StringBuilder` from `OperationLogPools.Journals`
    and target that instead - private, thread-local, nothing shared, so
    concurrent evaluation on different threads can't corrupt anything and
    there's no lock to leak if a hole throws.
  - `SynchronizedOperationLog.Append(ref handler)` locks `gate`, splices the
    rented buffer's content into `inner`'s real journal (via the same
    `IJournalOwner.BeginJournalEntry()`, walked with `StringBuilder.GetChunks()`
    and `StringBuilder.Append(ReadOnlySpan<char>)` to avoid an intermediate
    string), returns the buffer to `OperationLogPools.Journals`, then
    unlocks. `RootOperationLog`/`ChildOperationLog`'s own `Append` becomes a
    no-op - the handler already wrote everything directly into the real
    journal during evaluation.

  Two minor, deliberately-accepted residual risks, both in the same
  "self-healing, not catastrophic" category as the existing pooled-`char[]`
  leak noted above - worth documenting, not blocking on:
  - Unsynchronized path: if a hole throws mid-evaluation, whatever was
    already written straight into the *real* journal (timestamp prefix,
    prior literal segments) stays there as a truncated entry - no rollback.
    Arguably still useful ("this line was in progress when the exception
    hit"), but is new behavior versus today's "nothing journaled unless the
    string finished building."
  - Synchronized path: same scenario just loses the rented buffer to GC
    instead of returning it to the pool - a bounded, one-buffer loss, not a
    deadlock.
  - Reusing `OperationLogPools.Journals` for these buffers shifts that pool's
    usage pattern from "one rental per operation, held for its whole
    lifetime" to "one rental per `Append` call" on the synchronized path -
    functionally fine (it's `ConcurrentBag`-backed), just more churn than
    today; worth a tuning look (possibly a separate, smaller pool) when this
    is implemented rather than assumed to be optimal as-is.

**Feasibility analysis — `BeginSubOperation`:** initially this looked like it
conflicted with the feature as described: `operationName` isn't write-once
journal text - it's stored (`_operationName`) and reused repeatedly, in
`SetException`/`SetResult` (`Operation/ChildOperationLog.cs:13-25`),
`DisposeCore`'s "`` `Name` complete.``" line (`ChildOperationLog.cs:27-31`),
and (per item #4) escalation messages. If the interpolated handler writes
the name straight into the journal the way `Append`'s would, there's no
plain `string` left over to store as `_operationName` without an extra
splice-it-back-out allocation - seemingly undermining the "skip the
intermediate string" rationale.

But the actual value isn't the enabled path at all (where full evaluation
already happens regardless, and always will) - it's the **disabled** path:
a `DisabledOperationLog`, or a `SynchronizedOperationLog` wrapping one, can
have `handlerIsValid` bound to the new `IOperationLog.IsEnabled` (item #6),
so `log.BeginSubOperation($"payment-{ExpensiveComputation()}")` skips
`ExpensiveComputation()` entirely when the operation isn't journaling -
exactly the win `Append` gets, just for a call site that's arguably more
common (sub-operations are often started unconditionally on code paths that
don't know or care whether the parent is enabled). This was blocked by
`DisabledOperationLog`'s contract that `OperationName` stays accurate even
when disabled (`Operation/DisabledOperationLog.cs:10-14`) - skipping the
holes would silently produce an incomplete name, which is exactly the sort
of "correct when enabled, silently different when disabled" surprise this
library otherwise avoids.

**This is resolved by item #7 below (removing `IOperationLog.OperationName`
entirely).** With no public property promising an accurate name back, there's
nothing for a disabled sub-operation's skipped-evaluation name to violate,
and `DisabledOperationLog.BeginSubOperation` no longer needs to construct
(or even look at) a name at all - see #7's `return this;` simplification.
The enabled path is unaffected either way: `RootOperationLog`/
`ChildOperationLog` already fully evaluate the name today and continue to.

**Status:** Design settled for both `Append` and `BeginSubOperation` - the
`IJournalOwner`/rented-buffer approach above applies uniformly to both (the
same handler shape, just targeting different journal-write call sites).
Ready to implement, sequenced *after* item #7 (`OperationName` removal)
lands, since #7 is what makes `BeginSubOperation`'s disabled-path skip safe
and lets `DisabledOperationLog`'s implementation ignore the argument outright
rather than needing a splice-back-out fallback for the name.

## 4. `Escalate` writes a journal line, but only when it actually escalates

**Proposed:** `IOperationLog.Escalate(level)` should append a journal line
when it actually raises the operation's level (never when it's a no-op
because `level` isn't more severe than the current one). Root operations get
`` "Operation escalated to {level}." ``; sub-operations get
`` "`{subOperationName}` escalated to {level}." ``.

**Inferred rationale:** today, calling `log.Escalate(LogLevel.Warning)` (e.g.
for a rejected order that doesn't throw - the exact scenario called out in
`IOperationLog.Escalate`'s own doc comment, `Operation/IOperationLog.cs:69`)
leaves no trace of *why* the final entry ended up at a higher level than its
`BeginOperation` default - a reader sees `Warning` on the entry and has to
either already know why or search the journal text for a clue. An explicit
journal line closes that gap. Gating it on "only when it actually escalates"
avoids journal noise for the common defensive pattern of unconditionally
calling `Escalate` on every exit path (success and failure alike) rather
than branching just to decide whether to call it.

**Feasibility analysis:** straightforward, and fits the codebase's existing
root/sub-operation asymmetry pattern well:

- `OperationLogState.Escalate` (`Operation/OperationLogState.cs:73-77`)
  currently returns `void` and silently no-ops when `level <= Level`. It
  needs to report back whether it actually raised the level (return `bool`,
  or the previous `Level`) so the caller knows whether to journal anything.
- The message differs by root vs. sub-operation, which the shared base class
  can't produce today - `OperationLog<TSelf>.Escalate` (`Operation/OperationLog.cs:35-40`)
  has no notion of which one `TSelf` is. The existing precedent for this
  exact asymmetry is `SetException`/`SetResult`, which are *not* on the
  shared base at all - each is declared separately on `RootOperationLog`
  and `ChildOperationLog` (`Operation/RootOperationLog.cs:15-28`,
  `Operation/ChildOperationLog.cs:13-25`). `Escalate` should move the same
  way: out of `OperationLog<TSelf>` and into each derived class, both calling
  the same `_state.Escalate(level)` helper and, only if it returns `true`,
  appending their own message via `_state.BeginJournalEntry()`.
- `DisabledOperationLog.Escalate` (`Operation/DisabledOperationLog.cs:55`)
  needs no change - it stays a no-op, since nothing is journaling anyway.
- `SynchronizedOperationLog.Escalate` (`Operation/SynchronizedOperationLog.cs:45-50`)
  needs no change either, and - unlike the `Append`/`BeginSubOperation` case
  above - has no synchronization problem: it already locks `gate` before
  calling `inner.Escalate(level)`, and the new journal write happens *inside*
  that call, so it's covered by the same lock automatically.

**Decided:** the message includes the previous level, e.g.
`` "Operation escalated from Information to Warning." `` /
`` "`{subOperationName}` escalated from Information to Warning." ``. The
information is already available (`state.Level` right before the call) at
essentially no extra cost - `OperationLogState.Escalate` should capture it
before mutating `Level`, so it can be returned alongside (or instead of) the
`bool`/no-op signal, e.g. by returning the previous `LogLevel` and having the
caller compare it against the new `state.Level` to decide whether anything
changed.

**Status:** Feasible as described - ready to implement once prioritized.

## 5. Skip the redundant second `IsEnabled` check for interpolated-handler message overloads

**Proposed:** for the `Trace`/`Debug`/.../`Write` overloads whose `message`
parameter is one of the custom interpolated string handlers, stop calling
`logger.IsEnabled(...)` a second time in the method body. Instead, have each
handler expose whether it's enabled, and check that.

**Inferred rationale:** every generated handler-message overload currently
does this (`StructuredLoggerExtensions.g.cs:26-29`, one example of many):

```csharp
string messageText = message.ToStringAndClear();

if (!logger.IsEnabled(LogLevel.Trace))
    return;
```

but the handler's own constructor already computed exactly this
(`LogInterpolatedStringHandlers.g.cs:25-27`):

```csharp
public TraceInterpolatedStringHandler(int literalLength, int formattedCount, ILogger logger, out bool handlerIsValid)
{
    handlerIsValid = logger.IsEnabled(LogLevel.Trace);
```

`eng/GenerateSource.cs:488-489`'s own comment explains why
`ToStringAndClear()` must still run unconditionally (it releases the
handler's pooled buffer, and skipping that when disabled would leak it), but
that reasoning doesn't extend to the `IsEnabled` call itself - that's a
second, purely redundant dispatch (potentially virtual, and non-trivial for
`ILogger` implementations that consult filtering configuration) for
information the handler already has stored a moment earlier.

**Feasibility analysis:** fully feasible, small and mechanical, no blockers:

- Add a field to each handler struct in `LogInterpolatedStringHandlers.g.cs`,
  e.g. `private readonly bool _isEnabled;`, set from `handlerIsValid` in the
  constructor, exposed as `internal bool IsEnabled => _isEnabled;`.
- In `eng/GenerateSource.cs:501`, change
  `if (!logger.IsEnabled({group.LevelExpr}))` to `if (!message.IsEnabled)`
  for the `useHandler` branch only - the plain-`string message` overloads
  have no handler and must keep calling `logger.IsEnabled` directly (their
  `messageTextExpr`/`capturedPropertiesExpr` setup at
  `eng/GenerateSource.cs:495-499` already branches on `useHandler` for
  exactly this reason).
- Regenerate via `dotnet run eng/GenerateSource.cs` and commit the diff, per
  the standard workflow documented in CLAUDE.md.
- The `ToStringAndClear()` call stays unconditional, exactly as today, for
  the pooled-buffer reason already documented in the generator.

**Status:** Feasible as described, no open questions - ready to implement
once prioritized.

## 6. Add an `IsEnabled` property to `IOperationLog`

**Proposed:** expose a read-only `bool IsEnabled` on `IOperationLog`,
reporting whether the operation is actually journaling (i.e. `true` for a
`RootOperationLog`/`ChildOperationLog`, `false` for a `DisabledOperationLog`).

**Inferred rationale:** `DisabledOperationLog`'s doc comment already
describes the design intent this fits into - a disabled operation does "zero
journal accumulation," mirroring how the interpolated string handlers skip
evaluating their holes entirely when a level is disabled
(`Operation/DisabledOperationLog.cs:6-9`). But that skip only covers what the
library itself does internally (declining to append to the journal) - it
doesn't help a caller who does expensive work to compute a value *before*
passing it to `AddProperty`/`AppendValue`/`AppendJson`, none of which are
interpolated-handler-based and so can't lazily skip evaluating their
argument the way `logger.Debug($"... {ExpensiveCall()}")` can. Today there's
no way to ask "is this operation actually going to record anything?" before
doing that work, other than something like
`log.Properties.Count >= 0 /* always true, useless */` or checking for
`log is DisabledOperationLog` (not possible - the type is `internal`, and
doing so would be brittle even if it were public). Exposing `IsEnabled`
mirrors the long-familiar `ILogger.IsEnabled(level)` guard pattern, applied
to operation logging, e.g.:

```csharp
if (log.IsEnabled)
    log.AppendJson(BuildExpensiveDiagnosticSnapshot());
```

This also fits the same design language already used for `EventId`,
`OperationName`, and `Properties`/`AddProperty` on `DisabledOperationLog` -
"callers may read/use these regardless of whether the operation ends up
writing a log entry" (`Operation/DisabledOperationLog.cs:10-14`) - just
extended to let a caller find out *whether* it's disabled in the first
place, not only use the members that stay well-behaved either way.

**Feasibility analysis:** fully feasible, no blockers:

- `OperationLog<TSelf>` (`Operation/OperationLog.cs`) backs both
  `RootOperationLog` and `ChildOperationLog`, and both are only ever
  constructed once `logger.IsEnabled(level)` has already been confirmed true
  (`LoggerOperationExtensions.BeginOperation`) - so `IsEnabled` can be a
  hardcoded `true` on the shared base, no state lookup needed.
- `DisabledOperationLog.IsEnabled` (`Operation/DisabledOperationLog.cs`) is
  hardcoded `false`, alongside its existing hardcoded no-op members.
- `SynchronizedOperationLog.IsEnabled` (`Operation/SynchronizedOperationLog.cs`)
  passes through to `inner.IsEnabled` unlocked, the same way it already does
  for `EventId`/`OperationName` (`SynchronizedOperationLog.cs:27,29`) - a
  plain `bool` read needs no synchronization.
- Like `EventId`, this is naturally uniform across an entire operation tree:
  `BeginSubOperation` always returns the same kind of `IOperationLog` as its
  parent (`ChildOperationLog` from an enabled parent,
  another `DisabledOperationLog` from a disabled one -
  `Operation/DisabledOperationLog.cs:65`), so a sub-operation's `IsEnabled`
  always agrees with its root's. Worth documenting explicitly, the same way
  `EventId`'s doc comment does (`Operation/IOperationLog.cs:33-37`).
- No interaction with `Escalate` (item #4 above) - `IsEnabled` reflects only
  the up-front decision made at `BeginOperation`, which `Escalate` can never
  reverse (`IOperationLog.Escalate`'s doc comment already states this:
  "it never re-enables a disabled one," `Operation/IOperationLog.cs:65-66`).
- No naming collision with `ILogger.IsEnabled(LogLevel)` - different
  signature (parameterless property vs. a method taking a `LogLevel`), and a
  different interface, so both can be in scope without ambiguity.

**Status:** Feasible as described, no open questions - ready to implement
once prioritized. Note it's an interface addition, so it lands as a breaking
change for any external `IOperationLog` implementation (none are known to
exist outside this library today).

## 7. Remove `IOperationLog.OperationName`

**Decided:** cut the `OperationName` property from `IOperationLog` entirely.
Came out of analyzing item #3's `BeginSubOperation` handler overload - the
property was blocking that feature (see #3 above) and, on inspection, its
own justification was thin: unlike `EventId`, whose doc comment gives a
concrete reason to read it back ("tie a log line written elsewhere back to
the operation's own final entry," `IOperationLog.cs:35`), `OperationName`'s
doc comment never gave an example of why calling code would want to read a
name back that it already has in hand (it's the literal/variable the caller
just passed to `BeginOperation`/`BeginSubOperation`) - suggesting it was
added for symmetry with `EventId`/`Properties` rather than a real need.

**Impact beyond #3:**

- `IOperationLog.cs`: drop the `OperationName` property (`:47`) and its
  doc-comment cross-references - the class-level list of members still safe
  to call after root disposal (`:15`) and the contrast drawn in `EventId`'s
  doc comment (`:44`, "unlike `EventId` and `Properties`, this is specific to
  each level").
- `OperationLog<TSelf>` (`Operation/OperationLog.cs:26`): drop the public
  `OperationName` accessor. The `protected readonly string _operationName`
  field stays - `ChildOperationLog`/`RootOperationLog` still need it
  internally for journal text and the `Operation.Name` property
  (`RootOperationLog.cs:48`). This is purely about removing a way for
  external code to read the name back, not about changing what gets logged.
- `SynchronizedOperationLog.cs:29`: drop the one-line passthrough.
- **`DisabledOperationLog` simplifies substantially.** Today it exists as a
  two-part structure *specifically* because `OperationName` had to vary per
  level while `EventId`/`Properties` are shared across the whole tree: each
  instance carries its own `_operationName` plus a reference to a shared
  inner `State` class, and `BeginSubOperation` allocates a new
  `DisabledOperationLog` per call just to give the sub-operation its own name
  (`Operation/DisabledOperationLog.cs:22-38,65`). Once `OperationName` is
  gone, every `DisabledOperationLog` in a tree is behaviorally identical, so
  `BeginSubOperation` can become `return this;` - no allocation at all for
  sub-operations of a disabled root - and the private nested `State` class /
  two-constructor chain collapses into one flat class with no per-level
  state left to represent. This is a bigger win than anything in #3 on its
  own: it removes an entire axis of complexity (and a per-call allocation)
  from the disabled path, purely because `OperationName` is what was forcing
  it to exist.
- `test/RandomSkunk.StructuredLogging.Tests/OperationLoggingTests.cs`: delete
  the six tests that directly assert `OperationName`
  (`:42`, `:313-353`) - nothing left to assert.
- `CLAUDE.md`'s "Operation logging" section lists `OperationName` in the
  `IOperationLog` API-shape bullet - needs a matching edit when this ships.
- `BeginOperation`/`BeginSubOperation`'s `operationName` *parameter* is
  unaffected - it's still needed to seed the journal header, the "started"
  line, and (on the root) the final `Operation.Name` property. Only the
  ability to read it back out of an `IOperationLog` afterward goes away.

**Status:** Decided - ready to implement. Should land before or alongside
#3's `BeginSubOperation` handler overload, since it's what makes that
overload's disabled-path optimization safe (see #3 above).
