# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

RandomSkunk.StructuredLogging: modern structured logging extensions for .NET that separate human-readable messages from machine-readable attributes. It replaces `Microsoft.Extensions.Logging.Abstractions`'s `LogDebug`/`LogInformation`/etc. extension methods, which force every structured property into the message template. This library lets a message be formatted however the caller wants while structured properties are attached separately and explicitly.

## Environment

This is a WSL environment with no Linux .NET SDK installed. Use the Windows SDK via `cmd.exe`, e.g.:

```bash
cmd.exe /c "dotnet build"
cmd.exe /c "dotnet test"
```

The SDK version is pinned in `global.json` (currently 10.0.301).

## Commands

- Build: `dotnet build`
- Test (all projects, both TFMs): `dotnet test`
- Run a single test: `dotnet test --filter "FullyQualifiedName~StructuredLoggerExtensionsTests.Disabled_DoesNotEvaluateInterpolationHoles"`
- Pack the library for NuGet: `dotnet pack src/RandomSkunk.StructuredLogging`
- Regenerate the generated source files (see below): `dotnet run eng/GenerateSource.cs`, run from the repo root

## Architecture

- `src/RandomSkunk.StructuredLogging/` — the library, multi-targeted `net8.0;net10.0`. Ships two other kinds of assets alongside the compiled DLL: coding-agent guidance files (packed under `skillfiles/randomskunk-structuredlogging/`) and `build/RandomSkunk.StructuredLogging.targets` (packed to `buildTransitive/`), which installs those guidance files into a consuming project on build. `skill/SKILL.md` is the single source of truth for the guidance text (Claude Code's own skill file format); `eng/GenerateAgentFiles.cs` (a file-based app, run the same way as `eng/GenerateSource.cs`) strips its YAML frontmatter and re-wraps the body in the frontmatter each other tool expects, writing the results to `skillfiles-src/` (checked in, like the other generated files) — `agentfiles/cursor-rule.mdc` and `agentfiles/copilot-instructions.md` are the hand-authored frontmatter stubs it wraps the body in. The `.targets` file installs all four: `.claude/skills/randomskunk-structuredlogging/SKILL.md` (Claude Code), `.cursor/rules/randomskunk-structuredlogging.mdc` (Cursor, auto-attached on `**/*.cs`), `.github/instructions/randomskunk-structuredlogging.instructions.md` (GitHub Copilot, `applyTo: **/*.cs`), and — the odd one out — a delimited block merged into the consuming project's own `AGENTS.md` (Codex and other AGENTS.md-convention tools) via an inline `MergeRandomSkunkStructuredLoggingAgentsMd` `UsingTask`, since AGENTS.md is a file the project otherwise owns; the merge is idempotent (marker-delimited, replaces just that block on re-run) rather than a plain file copy. Each of the four installs has its own `RandomSkunkStructuredLoggingSkip*Install` opt-out property.
- `src/RandomSkunk.StructuredLogging.Analyzers/` — Roslyn analyzers + code fixes, a separate NuGet package (`RandomSkunk.StructuredLogging.Analyzers`), netstandard2.0-only (overrides Directory.Build.props' multi-targeting so it loads into any host: VS, VS Code/Roslyn, `dotnet build`). Packaged as analyzer-only (`IncludeBuildOutput=false`, `DevelopmentDependency=true`, dll under `analyzers/dotnet/cs`). The main library's `.csproj` carries a `Private="false" PrivateAssets="none"` `ProjectReference` to this project purely so packing it declares a matching `<dependency>` on the Analyzers package (auto-versioned) — installing `RandomSkunk.StructuredLogging` also installs the analyzers; it does not run them against the library's own code. See "The analyzers" below.
- `test/RandomSkunk.StructuredLogging.Tests/` — xUnit test project for the library, references it via `ProjectReference`. Uses AwesomeAssertions for assertions (`.Should()`), not xUnit's native `Assert`. `RecordingLogger` is a hand-rolled `ILogger` test double that captures the arguments of the last `Log<TState>` call. `PropertyTagCaptureTests`/`PropertyTagDestructuringTests` cover the `<PropertyName>`/`<@PropertyName>` tag formats.
- `test/RandomSkunk.StructuredLogging.Analyzers.Tests/` — xUnit test project for the analyzers, one `*AnalyzerTests`/`*CodeFixProviderTests` pair per analyzer (`RemoveLogPropertyTagFormatCodeFixProviderTests` covers RSSL0002's removal fix; `AddLogPropertyTagFormatMigration`, RSSL0003's add fix, is covered indirectly). `AnalyzerVerifier`/`CodeFixVerifier` are the shared test harnesses; `TestReferences`/`TestSource` hold shared compilation inputs.
- `RandomSkunk.StructuredLogging.slnx` — solution file (new XML-based `.slnx` format, not the legacy `.sln`).
- `Directory.Build.props` — shared MSBuild properties for all projects (target frameworks, nullable, implicit usings).
- `Directory.Packages.props` — central package management (`ManagePackageVersionsCentrally`); all `PackageReference`s in `.csproj` files omit `Version` and get it from here.
- `global.json` — pins the .NET SDK version.

Package metadata (`PackageId`, `Description`, `PackageLicenseExpression`, etc.) lives directly in each package's own `.csproj` — `src/RandomSkunk.StructuredLogging/RandomSkunk.StructuredLogging.csproj` and `src/RandomSkunk.StructuredLogging.Analyzers/RandomSkunk.StructuredLogging.Analyzers.csproj`.

### The public API shape

`StructuredLoggerExtensions` (generated) exposes, for each of `Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical`, plus a level-as-argument `Write`:

- 4 `EventId`/`Exception` combinations (both, `EventId` only, `Exception` only, neither)
- × 8 structured-property shapes: no properties, generic arity 1–6 via `(string Name, T1 Value) logProperty1, ...` (avoids boxing for small property counts), and a `params (string Name, object? Value)[]` overload (arbitrary count, boxed)
- × 2 collection variants: each of the 8 shapes above also has a sibling overload with a leading `IReadOnlyCollection<KeyValuePair<string, object?>> logProperties` parameter immediately after `this ILogger logger` (accepts a previously constructed `Dictionary<string, object?>` or `List<KeyValuePair<string, object?>>`), letting a caller combine that collection with statically-known properties in the same call. In the collection+params combo, the trailing `params` parameter is named `additionalLogProperties` to avoid colliding with the leading `logProperties` parameter.

= 448 methods total.

The message parameter is a custom `[InterpolatedStringHandler]` struct (one per level, plus a level-generic one for `Write`), not a plain `string`. Its constructor checks `logger.IsEnabled(level)` and, if disabled, sets `handlerIsValid = false` — the C# compiler then skips evaluating the interpolation holes entirely (they're never appended, never formatted), so `logger.Debug($"... {ExpensiveCall()}")` doesn't call `ExpensiveCall()` when the Debug level is disabled. A plain `string` argument implicitly converts to the handler type and always assumes the logger is enabled (no `IsEnabled` check, no formatting work — it's just wrapped as-is), matching the "no formatter caching, so a constant/non-constant string is never penalized" design goal.

#### `<PropertyName>` format tags

An interpolation hole's format can start with an "html-like" tag to *also* capture that value as a structured property: `$"Hello, {name:<UserName>}!"` appends `name` to the message text (using whatever format follows the tag, if any) and adds a `"UserName"` structured property with `name`'s raw (unformatted) value. `$"[{ts:<Timestamp>HH:mm:ss}]"` formats `ts` with `"HH:mm:ss"` in the message while capturing the raw `DateTime` under `"Timestamp"`. An empty tag (`<>`) opts out of capturing while still stripping itself out, so a real format that happens to start with `<` can be written as `<>therealformat`. A tag name starting with `@` (e.g. `<@UserId>`, or `<@>` to destructure without renaming) additionally renders the value into the *message* using Serilog-style destructured formatting instead of `IFormattable`/`ToString()` — any format text following such a tag is parsed but ignored, since destructured rendering replaces ordinary formatting entirely. Destructuring itself is implemented in `Internal/LogPropertyDestructuring.cs`: objects render as `TypeName { Prop1: Value1, ... }` (anonymous types omit the type name), collections as `[item1, item2]`, dictionaries as `{ [key1]: value1, ... }`, strings/chars are quoted, other scalars render unquoted via invariant-culture `IFormattable`/`ToString()`; recursion is depth-bounded, collections/dictionaries are capped per `MaxCollectionItems`, and reference cycles render `<circular reference>` instead of recursing forever. Parsing lives in `Internal/LogPropertyTagFormat.cs` (hand-authored, not generated — this is genuine parsing logic, not mechanical repetition):

- Fast path: if `format` is null/empty or doesn't start with `<`, return immediately — no allocation, not even a dictionary lookup.
- Tag path: format specifiers in interpolated strings are always compile-time literals (and therefore interned — the same literal text is always the same `string` instance process-wide), so the parsed `(PropertyName, Format)` result is cached in a `ConcurrentDictionary<string, TagFormat>` keyed by that literal reference. This is safe to cache unboundedly (unlike MEL's message-template cache) because the key space is bounded by the number of distinct tag literals actually written in source, not by runtime-constructed strings.
- Each handler struct's `AppendFormatted<T>(T value, [int alignment,] string? format)` overloads parse the tag, lazily allocate a `List<KeyValuePair<string, object?>>?` field on first capture (stays `null`, zero-alloc, if no tag is ever used), and pass the *stripped* remaining format on to `DefaultInterpolatedStringHandler` for the message text.
- `LogPropertiesState`/`LogPropertiesState<T1..T6>` were extended with an `explicitProperties` slot alongside the statically-typed property arguments (`property1..T6` for the generic overloads, the `params`/collection list for the non-generic one). For the plain (non-collection) overloads that slot is populated directly from the handler's `GetCapturedProperties()`; for the collection-parameter overloads it's a `ConcatPropertyList` of the caller-supplied collection followed by the tag-captured properties. `Count`/the indexer address that slot first, then the statically-typed properties — no list concatenation beyond the single `ConcatPropertyList` wrapper, just index arithmetic.

### Source layout for the API surface

- `Generated/LogInterpolatedStringHandlers.g.cs` — the 7 handler structs (one per level + `LogInterpolatedStringHandler` for `Write`). Nearly identical bodies; kept as 7 separate concrete types (rather than one generic type) because `InterpolatedStringHandlerArgumentAttribute` can only bind constructor parameters to actual method parameters, and the level-specific methods (`Debug`, `Information`, ...) don't have a `LogLevel` parameter to bind to — each handler type hardcodes its own level instead. Each wraps a `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler` field (invariant culture) and delegates `AppendLiteral`/`AppendFormatted` straight to it — that type already implements pooled-buffer building, `IFormattable`/`ISpanFormattable` dispatch, and alignment padding correctly, so there's no need to hand-roll it. Because `DefaultInterpolatedStringHandler` is itself a ref struct, these wrapper types must be `ref struct` too (and *not* `readonly struct` — a readonly struct defensively copies mutable-struct fields on every method call, which would silently make `Append*` mutate a throwaway copy instead of the real field).
- `Generated/LogPropertiesState.g.cs` — `LogPropertiesState<T1..T6>` (internal), one generic arity per file section. Each closed generic instantiation gets its own `static readonly Func<TState, Exception?, string> Formatter` field, which the CLR caches per-instantiation for free — this is what replaces `Microsoft.Extensions.Logging`'s fragile format-string-keyed formatter cache (the thing that made non-constant message templates a performance foot-gun in the original MEL extension methods).
- `Generated/StructuredLoggerExtensions.g.cs` — the 448 extension methods.
- `Internal/LogPropertiesState.cs` — the non-generic state type backing the 0-arity and `params` array overloads (with or without the leading collection parameter).
- `Internal/TuplePropertyList.cs` — zero-copy `IReadOnlyList<KeyValuePair<string, object?>>` adapter over a `(string Name, object? Value)[]`, used by the `params` overload.
- `Internal/ConcatPropertyList.cs` — zero-copy `IReadOnlyList<KeyValuePair<string, object?>>` adapter that concatenates two lists without copying; used by the collection-parameter overloads to combine the caller-supplied collection (first) with tag-captured properties (second) before handing them to `LogPropertiesState`/`LogPropertiesState<T1..T6>`.
- `Internal/LogPropertyTagFormat.cs` — parses `<PropertyName>format` (and `<@PropertyName>`-destructuring) tags out of a format string; see below.
- `Internal/LogPropertyDestructuring.cs` — renders a value as Serilog-style destructured text for the `<@PropertyName>` tag format; see below.

All three `Generated/*.g.cs` files are produced by `eng/GenerateSource.cs`, a .NET 10 file-based app (no `.csproj`; run directly via `dotnet run eng/GenerateSource.cs`). The generated files are checked into source control like ordinary source — re-run the generator and commit the diff after changing anything in `eng/GenerateSource.cs` (e.g. to change the max generic arity, add a level, etc.). The generator also emits XML doc comments for every public generated member (mechanically derived from level/arity/EventId-Exception-combo, e.g. "Writes a log message with 2 structured properties at the Debug level."); `GenerateDocumentationFile` is on with no `CS1591` suppression, so a missing doc comment on any public member fails the build with a warning-as-visible signal (not error) the next time source changes.

### The analyzers (`src/RandomSkunk.StructuredLogging.Analyzers/`)

Five diagnostics, `RSSL0001`–`RSSL0005`, all defined in `DiagnosticDescriptors.cs`:

- **RSSL0001** (`AvoidLoggerExtensionsAnalyzer`/`AvoidLoggerExtensionsCodeFixProvider`, Info) — flags calls to `Microsoft.Extensions.Logging.LoggerExtensions`' `Log`/`LogTrace`/`LogDebug`/`LogInformation`/`LogWarning`/`LogError`/`LogCritical`, which force structured properties into the message template, and offers a fix (`LoggerExtensionsMigration`) converting to the corresponding RandomSkunk.StructuredLogging call.
- **RSSL0002** (`LogPropertyTagFormatAnalyzer`/`LogPropertyTagFormatCodeFixProvider`, Hidden) — anchors on an interpolation hole that *already* uses the `<PropertyName>` tag format; its fix (`RemoveLogPropertyTagFormatMigration`) strips the tag back to a plain hole.
- **RSSL0003** (`NonCapturingInterpolationHoleAnalyzer`/`NonCapturingInterpolationHoleCodeFixProvider`, Hidden) — the mirror image of RSSL0002: anchors on a hole that does *not* use the tag format; its fix (`AddLogPropertyTagFormatMigration`) adds one, guessing a property name via `PropertyNameGuessing` (always falls back to a generic placeholder, since the guess only seeds a tag the developer can rename).
- **RSSL0004** (`LogPropertyTupleArgumentAnalyzer`/`LogPropertyTupleArgumentCodeFixProvider`, Hidden) — anchors on a trailing `("Name", value)` tuple argument whose name is a compile-time-constant string (per the broader "constant" notion in `ConstantStringExpressionParsing.cs`, which also covers interpolated strings whose holes are themselves constant); its fix (`MoveLogPropertyTupleArgumentMigration`) moves it into a `<PropertyName>` tag on an interpolation hole instead.
- **RSSL0005** (`StructuredLoggerExtensionsInvocationAnalyzer`/`StructuredLoggerExtensionsInvocationCodeFixProvider`, Hidden) — anchors on *any* call to a RandomSkunk.StructuredLogging extension method regardless of overload; its fix (`StructuredLoggerExtensionsInvocationMigration`) converts back to the corresponding `Microsoft.Extensions.Logging` call, using `PropertyNameGuessing` for property names read from source (no generic fallback here — a wrong/generic name would become a permanent, visible part of the message template).

RSSL0002–RSSL0005 are all `Hidden`-severity by design: they don't flag anything wrong with the code, they exist purely as anchor locations so their paired code fixes can be invoked from an IDE. `PropertyNameGuessing.cs` and `ConstantStringExpressionParsing.cs` are shared helpers, not tied to one analyzer. `AnalyzerReleases.Shipped.md`/`AnalyzerReleases.Unshipped.md` track the required Roslyn analyzer-release-tracking metadata for these five IDs.
