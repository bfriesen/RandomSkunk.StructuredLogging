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

- `src/RandomSkunk.StructuredLogging/` — the library, multi-targeted `net8.0;net10.0`.
- `test/RandomSkunk.StructuredLogging.Tests/` — xUnit test project, references the library via `ProjectReference`. Uses AwesomeAssertions for assertions (`.Should()`), not xUnit's native `Assert`. `RecordingLogger` is a hand-rolled `ILogger` test double that captures the arguments of the last `Log<TState>` call.
- `RandomSkunk.StructuredLogging.slnx` — solution file (new XML-based `.slnx` format, not the legacy `.sln`).
- `Directory.Build.props` — shared MSBuild properties for all projects (target frameworks, nullable, implicit usings).
- `Directory.Packages.props` — central package management (`ManagePackageVersionsCentrally`); all `PackageReference`s in `.csproj` files omit `Version` and get it from here.
- `global.json` — pins the .NET SDK version.

Package metadata (`PackageId`, `Description`, `PackageLicenseExpression`, etc.) lives directly in `src/RandomSkunk.StructuredLogging/RandomSkunk.StructuredLogging.csproj`.

### The public API shape

`StructuredLoggerExtensions` (generated) exposes, for each of `Trace`/`Debug`/`Information`/`Warning`/`Error`/`Critical`, plus a level-as-argument `Write`:

- 4 `EventId`/`Exception` combinations (both, `EventId` only, `Exception` only, neither)
- × 8 structured-property shapes: no properties, generic arity 1–6 via `(string Name, T1 Value) logProperty1, ...` (avoids boxing for small property counts), and a `params (string Name, object? Value)[]` overload (arbitrary count, boxed)
- × 2 collection variants: each of the 8 shapes above also has a sibling overload with a leading `IReadOnlyCollection<KeyValuePair<string, object?>> logProperties` parameter immediately after `this ILogger logger` (accepts a previously constructed `Dictionary<string, object?>` or `List<KeyValuePair<string, object?>>`), letting a caller combine that collection with statically-known properties in the same call. In the collection+params combo, the trailing `params` parameter is named `additionalLogProperties` to avoid colliding with the leading `logProperties` parameter.

= 448 methods total.

The message parameter is a custom `[InterpolatedStringHandler]` struct (one per level, plus a level-generic one for `Write`), not a plain `string`. Its constructor checks `logger.IsEnabled(level)` and, if disabled, sets `handlerIsValid = false` — the C# compiler then skips evaluating the interpolation holes entirely (they're never appended, never formatted), so `logger.Debug($"... {ExpensiveCall()}")` doesn't call `ExpensiveCall()` when the Debug level is disabled. A plain `string` argument implicitly converts to the handler type and always assumes the logger is enabled (no `IsEnabled` check, no formatting work — it's just wrapped as-is), matching the "no formatter caching, so a constant/non-constant string is never penalized" design goal.

#### `<PropertyName>` format tags

An interpolation hole's format can start with an "html-like" tag to *also* capture that value as a structured property: `$"Hello, {name:<UserName>}!"` appends `name` to the message text (using whatever format follows the tag, if any) and adds a `"UserName"` structured property with `name`'s raw (unformatted) value. `$"[{ts:<Timestamp>HH:mm:ss}]"` formats `ts` with `"HH:mm:ss"` in the message while capturing the raw `DateTime` under `"Timestamp"`. An empty tag (`<>`) opts out of capturing while still stripping itself out, so a real format that happens to start with `<` can be written as `<>therealformat`. Parsing lives in `Internal/LogPropertyTagFormat.cs` (hand-authored, not generated — this is genuine parsing logic, not mechanical repetition):

- Fast path: if `format` is null/empty or doesn't start with `<`, return immediately — no allocation, not even a dictionary lookup.
- Tag path: format specifiers in interpolated strings are always compile-time literals (and therefore interned — the same literal text is always the same `string` instance process-wide), so the parsed `(PropertyName, Format)` result is cached in a `ConcurrentDictionary<string, TagFormat>` keyed by that literal reference. This is safe to cache unboundedly (unlike MEL's message-template cache) because the key space is bounded by the number of distinct tag literals actually written in source, not by runtime-constructed strings.
- Each handler struct's `AppendFormatted<T>(T value, [int alignment,] string? format)` overloads parse the tag, lazily allocate a `List<KeyValuePair<string, object?>>?` field on first capture (stays `null`, zero-alloc, if no tag is ever used), and pass the *stripped* remaining format on to `DefaultInterpolatedStringHandler` for the message text.
- `LogPropertiesState`/`LogPropertiesState<T1..T6>` were extended with a `capturedProperties` slot (populated from the handler's `GetCapturedProperties()`) alongside the explicitly-passed properties; `Count`/the indexer address captured properties first, then explicit ones — no list concatenation, just index arithmetic.

### Source layout for the API surface

- `Generated/LogInterpolatedStringHandlers.g.cs` — the 7 handler structs (one per level + `LogInterpolatedStringHandler` for `Write`). Nearly identical bodies; kept as 7 separate concrete types (rather than one generic type) because `InterpolatedStringHandlerArgumentAttribute` can only bind constructor parameters to actual method parameters, and the level-specific methods (`Debug`, `Information`, ...) don't have a `LogLevel` parameter to bind to — each handler type hardcodes its own level instead. Each wraps a `System.Runtime.CompilerServices.DefaultInterpolatedStringHandler` field (invariant culture) and delegates `AppendLiteral`/`AppendFormatted` straight to it — that type already implements pooled-buffer building, `IFormattable`/`ISpanFormattable` dispatch, and alignment padding correctly, so there's no need to hand-roll it. Because `DefaultInterpolatedStringHandler` is itself a ref struct, these wrapper types must be `ref struct` too (and *not* `readonly struct` — a readonly struct defensively copies mutable-struct fields on every method call, which would silently make `Append*` mutate a throwaway copy instead of the real field).
- `Generated/LogPropertiesState.g.cs` — `LogPropertiesState<T1..T6>` (internal), one generic arity per file section. Each closed generic instantiation gets its own `static readonly Func<TState, Exception?, string> Formatter` field, which the CLR caches per-instantiation for free — this is what replaces `Microsoft.Extensions.Logging`'s fragile format-string-keyed formatter cache (the thing that made non-constant message templates a performance foot-gun in the original MEL extension methods).
- `Generated/StructuredLoggerExtensions.g.cs` — the 448 extension methods.
- `Internal/LogPropertiesState.cs` — the non-generic state type backing the 0-arity and `params` array overloads (with or without the leading collection parameter).
- `Internal/TuplePropertyList.cs` — zero-copy `IReadOnlyList<KeyValuePair<string, object?>>` adapter over a `(string Name, object? Value)[]`, used by the `params` overload.
- `Internal/ConcatPropertyList.cs` — zero-copy `IReadOnlyList<KeyValuePair<string, object?>>` adapter that concatenates two lists without copying; used by the collection-parameter overloads to combine tag-captured properties with the caller-supplied collection before handing them to `LogPropertiesState`/`LogPropertiesState<T1..T6>`.
- `Internal/LogPropertyTagFormat.cs` — parses `<PropertyName>format` tags out of a format string; see below.

All three `Generated/*.g.cs` files are produced by `eng/GenerateSource.cs`, a .NET 10 file-based app (no `.csproj`; run directly via `dotnet run eng/GenerateSource.cs`). The generated files are checked into source control like ordinary source — re-run the generator and commit the diff after changing anything in `eng/GenerateSource.cs` (e.g. to change the max generic arity, add a level, etc.). The generator also emits XML doc comments for every public generated member (mechanically derived from level/arity/EventId-Exception-combo, e.g. "Writes a log message with 2 structured properties at the Debug level."); `GenerateDocumentationFile` is on with no `CS1591` suppression, so a missing doc comment on any public member fails the build with a warning-as-visible signal (not error) the next time source changes.
