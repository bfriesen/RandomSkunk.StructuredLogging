# What's Next: Feedback, Roadmap, and Wrapping Up the Series

That's the series. Nine posts, one library, two features. Here's a quick recap of the ground
covered, where to send feedback, and what's on the horizon.

## The series, in one paragraph each

[Post 1](01-introducing-randomskunk-structuredlogging.md) introduced RandomSkunk.StructuredLogging:
messages and structured properties are two different concerns, and this library lets you write each
one explicitly instead of forcing both into one template string.
[Post 2](02-why-log-messages-shouldnt-contain-your-data.md) made the broader case for that
separation — the failure modes that show up once one string is asked to be both a human-readable
message and a machine-queryable template.

Posts [3](03-property-tags-deep-dive.md) and [4](04-zero-cost-logging-interpolated-handlers.md) went
deep on the mechanics: the `<PropertyName>`/`<@PropertyName>` tag format that captures a value
without writing it twice, and the `[InterpolatedStringHandler]` machinery that makes a disabled
`logger.Debug($"... {ExpensiveCall()}")` skip evaluating `ExpensiveCall()` entirely, no manual
`IsEnabled` guard required. [Post 5](05-migrating-with-analyzers.md) covered the analyzers package —
`RSSL0001` and its code fix to convert existing `Microsoft.Extensions.Logging` calls automatically,
plus the analyzers that catch the eager-evaluation trap that undoes the whole optimization if an
interpolated string gets routed through a variable, a method call, or a helper first.

Posts [6](06-canonical-log-lines-operation-logging.md) through
[9](09-performance-internals-pooling-and-thread-safety.md) turned to operation logging: journaling
an entire operation — and any nested sub-operations — into one log entry instead of a scattered
trail of individual lines, the root-vs-sub-operation asymmetry in `SetResult`/`SetException`/
`Escalate`, a full before/after refactor ([post 8](08-refactoring-case-study.md)), and the pooling
and opt-in thread-safety design that keeps it cheap under real load.

## Where to send feedback

- **Issues and bugs**: [github.com/bfriesen/RandomSkunk.StructuredLogging/issues](https://github.com/bfriesen/RandomSkunk.StructuredLogging/issues)
- **The package**: [nuget.org/packages/RandomSkunk.StructuredLogging](https://www.nuget.org/packages/RandomSkunk.StructuredLogging)
- **The analyzers package**: [nuget.org/packages/RandomSkunk.StructuredLogging.Analyzers](https://www.nuget.org/packages/RandomSkunk.StructuredLogging.Analyzers)

If something in this series didn't match what you saw when you tried it, or a pattern from your own
codebase doesn't map cleanly onto anything covered here, that's exactly the kind of thing worth
opening an issue about.

## What's on the horizon

A few things under active consideration for future releases:

- **Making the trailing format after a `<@PropertyName>` destructuring tag meaningful**, instead of
  silently discarding it as it does today — letting the message text apply a real format (e.g.
  `F3`) while the captured property still signals to a downstream sink that the raw value should be
  destructured. This is a real behavior change, not just an addition, so it'll come with its own
  version bump and a closer look at the exact rules once it's settled.
- **A couple of small "unnecessary tag" analyzers** — for instance, flagging a `<>` no-capture
  escape hatch used in a case where it isn't actually needed (nothing about the surrounding format
  starts with `<`), since it's easy to reach for out of habit even where it does nothing.

Nothing here is committed to a release yet — if any of it would materially affect how you're using
the library today, the issue tracker above is the place to weigh in before it ships.

## Thanks for reading

If you're adopting the library off the back of this series, [post 5](05-migrating-with-analyzers.md)
is the fastest path in for an existing `Microsoft.Extensions.Logging` codebase, and
[post 6](06-canonical-log-lines-operation-logging.md) is the best starting point if operation
logging is the part that caught your interest. Either way — welcome, and let us know how it goes.
