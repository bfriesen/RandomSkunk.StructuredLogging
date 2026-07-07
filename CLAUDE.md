# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

RandomSkunk.StructuredLogging: modern structured logging extensions for .NET that separate human-readable messages from machine-readable attributes. The goal is to let contextual data (user IDs, correlation IDs, metrics) be attached to logs without forcing it into message templates.

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
- Run a single test: `dotnet test --filter "FullyQualifiedName~PlaceholderTests.Placeholder"`
- Pack the library for NuGet: `dotnet pack src/RandomSkunk.StructuredLogging`

## Architecture

- `src/RandomSkunk.StructuredLogging/` — the library, multi-targeted `net8.0;net10.0`. Currently empty (no types yet).
- `test/RandomSkunk.StructuredLogging.Tests/` — xUnit test project, references the library via `ProjectReference`. Uses AwesomeAssertions for assertions (`.Should()`), not xUnit's native `Assert`.
- `RandomSkunk.StructuredLogging.slnx` — solution file (new XML-based `.slnx` format, not the legacy `.sln`).
- `Directory.Build.props` — shared MSBuild properties for all projects (target frameworks, nullable, implicit usings).
- `Directory.Packages.props` — central package management (`ManagePackageVersionsCentrally`); all `PackageReference`s in `.csproj` files omit `Version` and get it from here.
- `global.json` — pins the .NET SDK version.

Package metadata (`PackageId`, `Description`, `PackageLicenseExpression`, etc.) lives directly in `src/RandomSkunk.StructuredLogging/RandomSkunk.StructuredLogging.csproj`.
