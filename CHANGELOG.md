# RandomSkunk.StructuredLogging

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog],
and this project adheres to [Semantic Versioning].

## [0.9.5] - 2026-05-05

- Bug fix: don't pool property lists. It interferes with testing.

## [0.9.4] - 2026-05-05

- Rename `Parameters` property in `OperationLog` and `IOperationLog` to `Properties` and change its type to `List<KeyValuePair<string, object?>>`, which is lazily initialized.
- Remove `OperationLog.IsNotNull`, `OperationLog.IsNotNullOrEmpty`, and `OperationLog.IsNotNullOrWhiteSpace` methods.
- Remove `LogOperation` overloads with generic list parameter.
- Don't send "operation starting" log in `OperationLog` constructor.
- Add abstract `TestOperationLog` class, suitable for mocking the `IOperationLog` interface.
- Update the `LogPropertyTuple` structs so uninitialized instances have a count of zero.

## [0.9.3] - 2026-04-22

- Fix null issues.
- Add `OperationLog.Value` and `OperationLog.JsonValue` methods.
- Add `IOperationLog` interface.

## [0.9.2] - 2026-04-09

- Rename `Operation` struct to `OperationLog`.
- Add `OperationLog.IsNotNull`, `OperationLog.IsNotNullOrEmpty`, and `OperationLog.IsNotNullOrWhiteSpace` methods.
- Rename `OperationLog.Log` to `OperationLog.Append` and `OperationLog.Return` to `OperationLog.ReturnValue`.
- Add caller argument parameter to `OperationLog.ReturnValue` and `OperationLog.Exception` methods.
- Adjust operation log messages.

## [0.9.1] - 2026-04-09

Add operation logging feature.

## [0.9.0] - 2026-02-11

Initial implementation of RandomSkunk.StructuredLogging.

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/
[Semantic Versioning]: https://semver.org/spec/v2.0.0.html
[0.9.5]: ../../compare/v0.9.4...v0.9.5
[0.9.4]: ../../compare/v0.9.3...v0.9.4
[0.9.3]: ../../compare/v0.9.2...v0.9.3
[0.9.2]: ../../compare/v0.9.1...v0.9.2
[0.9.1]: ../../compare/v0.9.0...v0.9.1
[0.9.0]: ../../compare/v0.0.0...v0.9.0
