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
- × 9 structured-property shapes: no properties, generic arity 1–6 via `(string Name, T1 Value) logProperty1, ...` (avoids boxing for small property counts), a `params (string Name, object? Value)[]` overload (arbitrary count, boxed), and an `IReadOnlyCollection<KeyValuePair<string, object?>>` overload (accepts a `Dictionary<string, object?>` or `List<KeyValuePair<string, object?>>` directly)

= 252 methods total.

The message parameter is a custom `[InterpolatedStringHandler]` struct (one per level, plus a level-generic one for `Write`), not a plain `string`. Its constructor checks `logger.IsEnabled(level)` and, if disabled, sets `handlerIsValid = false` — the C# compiler then skips evaluating the interpolation holes entirely (they're never appended, never formatted), so `logger.Debug($"... {ExpensiveCall()}")` doesn't call `ExpensiveCall()` when the Debug level is disabled. A plain `string` argument implicitly converts to the handler type and always assumes the logger is enabled (no `IsEnabled` check, no formatting work — it's just wrapped as-is), matching the "no formatter caching, so a constant/non-constant string is never penalized" design goal.

### Source layout for the API surface

- `Generated/LogInterpolatedStringHandlers.g.cs` — the 7 handler structs (one per level + `LogInterpolatedStringHandler` for `Write`). Nearly identical bodies; kept as 7 separate concrete types (rather than one generic type) because `InterpolatedStringHandlerArgumentAttribute` can only bind constructor parameters to actual method parameters, and the level-specific methods (`Debug`, `Information`, ...) don't have a `LogLevel` parameter to bind to — each handler type hardcodes its own level instead.
- `Generated/LogPropertiesState.g.cs` — `LogPropertiesState<T1..T6>` (internal), one generic arity per file section. Each closed generic instantiation gets its own `static readonly Func<TState, Exception?, string> Formatter` field, which the CLR caches per-instantiation for free — this is what replaces `Microsoft.Extensions.Logging`'s fragile format-string-keyed formatter cache (the thing that made non-constant message templates a performance foot-gun in the original MEL extension methods).
- `Generated/StructuredLoggerExtensions.g.cs` — the 252 extension methods.
- `Internal/LogPropertiesState.cs` — the non-generic state type backing the 0-arity, `params` array, and collection overloads.
- `Internal/TuplePropertyList.cs` — zero-copy `IReadOnlyList<KeyValuePair<string, object?>>` adapter over a `(string Name, object? Value)[]`, used by the `params` overload.

All three `Generated/*.g.cs` files are produced by `eng/GenerateSource.cs`, a .NET 10 file-based app (no `.csproj`; run directly via `dotnet run eng/GenerateSource.cs`). The generated files are checked into source control like ordinary source — re-run the generator and commit the diff after changing anything in `eng/GenerateSource.cs` (e.g. to change the max generic arity, add a level, etc.). `GenerateDocumentationFile` is on for packaging, so the src project suppresses `CS1591` (missing XML doc comment) since the generated overloads don't have per-method docs yet.
