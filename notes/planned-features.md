# Planned features

Working notes on features that are planned but not yet designed/implemented in
detail. Unlike `misuse-scenarios.md` (surprising/buggy existing behavior),
these are intentional additions to the API surface or behavior.

Items are **never renumbered** - several sections cross-reference each other by
number. A section is deleted once its work ships, so the numbering has gaps:
items 3-7 (interpolated-handler overloads for `Append`/`BeginSubOperation`,
`Escalate`'s conditional journal line, skipping the redundant second
`IsEnabled` check, `IOperationLog.IsEnabled`, and removing
`IOperationLog.OperationName`) all shipped and were removed, as did the core
behavior of item #1. Their design notes are in git history.

| # | Item | Status |
|---|------|--------|
| 1 | Analyzer: flag an ambiguous `<@>` tag with a trailing format | **Open** - not started |
| 2 | Analyzer: flag an unnecessary `<>` no-capture escape hatch | **Open** - not started |

Both remaining items are analyzers for confusing-but-legal tag formats, and
neither has been started.

## 1. Analyzer: flag an ambiguous `<@>` destructuring tag with a trailing format

The behavior this depends on has shipped: trailing format text after a
`<@PropertyName>` tag is honored for the message (so `{amount:<@Amount>F3}`
formats the message with `F3` and captures the property as `@Amount`, for a
downstream sink to destructure), and an empty destructuring tag with a
trailing format (`<@>F3`) behaves exactly like the plain `<>F3` escape hatch,
since there's no property name left for the `@` to attach to.

What's still open is an analyzer for that last form. `{amount:<@>F3}` is
inherently ambiguous to a reader - it isn't obvious at a glance whether the
trailing format is honored or discarded, since an empty property name and
destructuring are stacked together. The combination of "empty destructuring
tag" + "trailing format" should be flagged (warning severity - this isn't a
runtime bug like RSSL0008, just a confusing construct with a strictly clearer
equivalent) so the developer simplifies to the format they actually want:

- If they want the format honored and no destructuring: write `{amount:<>F3}`.
- If they want destructuring and no format: write `{amount:<@>}`, with the
  format text removed entirely.

**Code fix:** offer both choices explicitly - one arm rewriting to `<>F3`, the
other to `<@>` with the format text deleted - letting the developer pick which
behavior they meant rather than requiring them to remember which one the
ambiguous form silently resolves to.

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
