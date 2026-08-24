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
