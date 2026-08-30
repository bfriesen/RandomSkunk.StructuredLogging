# Changelog

All notable changes to this project are documented here. This project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Both packages — `RandomSkunk.StructuredLogging` and `RandomSkunk.StructuredLogging.Analyzers` —
are versioned and released together.

## 1.0.0 - 2026-08-30

First stable release. The API is now considered settled; subsequent 1.x releases will not break it.

### Added

- **Operation logging** (`RandomSkunk.StructuredLogging.Operation`) — journal everything that
  happens during an operation and any nested sub-operations, then flush it as exactly *one* log
  entry when the operation completes. `logger.BeginOperation("name")` returns an `IOperationLog`;
  disposing it writes the entry. Includes `BeginSubOperation`, `AddProperty`, `Append`/`AppendValue`/
  `AppendJson`, `SetException`/`SetResult` (root only) and `AppendException`/`AppendResult`,
  `Escalate`, `IsEnabled`, `EventId`, `Properties`, opt-in thread safety via
  `BeginOperation(..., threadSafe: true)`, interpolated-string-handler overloads for `Append`/
  `BeginSubOperation`, and the chainable `SetResultTo`/`AddPropertyTo`/`AppendValueTo`/
  `AppendResultTo`/`AppendJsonTo` extension methods.
- **Five new analyzers.** `RSSL0006` (undisposed `IOperationLog`, with two code fixes), and
  `RSSL0007`/`RSSL0009`/`RSSL0010` (a `<PropertyName>`-capturing interpolated string that gets
  routed through a local, an expression, or a helper method, silently defeating both the tag and
  the disabled-level optimization). `RSSL0008` (an unterminated `<PropertyName>` tag) ships at
  **error** severity.
- A plain `string message` overload for every level/`EventId`/`Exception`/property combination, so
  a non-interpolated message binds directly instead of constructing an interpolated-string handler.
- Trimming and Native AOT support: the library is marked `IsAotCompatible`, and `AppendJson` — its
  only API that isn't AOT safe — is annotated `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`,
  so it warns at the call site instead of failing at run time. See the README's
  "Trimming and Native AOT" section.
- Every analyzer diagnostic now carries a `helpLinkUri` pointing at its row in the README.

### Changed

- **Breaking:** the `LogInterpolatedStringHandler` type is now named
  `WriteInterpolatedStringHandler`, matching the `Write` method it serves.
- **Breaking:** the implicit `string`-to-handler conversion was replaced with the dedicated `string`
  overloads above, and handler-typed `message` parameters are now passed by `ref`.
- **Breaking:** an interpolation hole whose format starts with `<` and never closes (e.g.
  `{userId:<UserId}`) now throws `UnterminatedLogPropertyTagException` instead of being passed
  through as a literal format string. `RSSL0008` catches the same mistake at compile time. A real
  format that must start with `<` uses the `<>` escape hatch.
- **Breaking:** format text following a `<@PropertyName>` destructuring tag (e.g. `<@Amount>F3`) is
  now honored for the message text instead of being discarded; destructuring then affects only how
  the property is captured.
- Structured property ordering is now pinned and identical across every combining shape: a
  caller-supplied collection first, then `<PropertyName>`-captured properties, then per-call tuple
  properties. The 0-arity overloads previously ordered the first two groups the other way around,
  which flipped duplicate-key precedence for sinks that resolve by first- or last-wins.
- Substantial allocation and throughput work across the hot path: the interpolated message is built
  only after the level check, a handler-message overload calls `ILogger.IsEnabled` once rather than
  twice, destructured values render without an intermediate string or a per-call `HashSet`, journal
  values are appended in place rather than rendered to a string first, and the operation-log pool no
  longer reads `ConcurrentBag<T>.Count` on its return path.
- The analyzers package is now built against Roslyn 4.8.0 rather than 5.6.0, so it loads in any
  host with the .NET 8 SDK or VS 2022 17.8 and newer. Built against 5.6.0, it failed to load in an
  older host with `CS9057`, silently costing that consumer all ten diagnostics and every code fix.
- The `Microsoft.Extensions.Logging.Abstractions` floor is now per-target-framework - 8.0.0 for
  `net8.0`, 10.0.0 for `net10.0` - instead of 10.0.9 for both, so installing this package no longer
  drags a .NET 8 application onto the 10.x abstractions.
- `AssemblyVersion` is pinned to `<major>.0.0.0`, so every 1.x build shares one assembly identity
  and a patch or minor upgrade is a binary drop-in. `FileVersion`/`InformationalVersion` still carry
  the full version.
- The assemblies are not strong-named, and deliberately so - the strong-name reference rule is a
  .NET Framework restriction that does not apply to this library's `net8.0`/`net10.0` targets.

### Removed

- **Breaking:** the `params (string Name, object? Value)[]` overloads. A call with more than six
  statically-known properties builds a collection and uses the collection overload instead — the
  `params` form boxed every value and hid the allocation at the call site.
- **Breaking:** the `build/RandomSkunk.StructuredLogging.targets` file and the bundled coding-agent
  skill that shipped inside the 0.10.0 package.

## 0.10.0 and earlier

Pre-release versions, published while the API was still moving. `RandomSkunk.StructuredLogging`
0.9.0-0.9.5 and 0.10.0 and `RandomSkunk.StructuredLogging.Analyzers` 0.10.0 are on nuget.org but are
not supported; upgrade to 1.0.0.
