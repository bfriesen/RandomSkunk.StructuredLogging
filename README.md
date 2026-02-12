# RandomSkunk.StructuredLogging

[![NuGet](https://img.shields.io/nuget/v/RandomSkunk.StructuredLogging.svg)](https://www.nuget.org/packages/RandomSkunk.StructuredLogging/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Modern, high-performance structured logging extensions for .NET that cleanly separate human-readable messages from machine-readable properties. Stop cluttering your message templates with structured data and start writing logs that are easier to read and query.

## Why Choose RandomSkunk.StructuredLogging?

Traditional structured logging forces you to embed data into message templates. This often leads to:

- **Verbose and Unreadable Messages**: `Log("User {UserId} logged in from {IPAddress}", userId, ipAddress)`
- **Performance Overhead**: Message template caching can consume memory and CPU.
- **Rigid Structure**: You can only log what your template allows.

This library takes a different approach by treating messages and properties as separate concerns, giving you the best of both worlds: **clean, readable messages** and **rich, queryable data**.

## Features

- ✨ **Clean Separation**: Keep your log messages for humans and your properties for machines.
- 🚀 **High Performance**: A design that avoids message template caching overhead.
- 📝 **Powerful Interpolated Strings**: Automatically extract properties from interpolated strings (`$"User {user.Name:<UserName>}"`) without sacrificing performance. The interpolation only happens if the log level is enabled!
- 💪 **Flexible & Type-Safe**: Pass properties using tuples, dictionaries, or arrays with a rich set of overloads.

## Quick Start

### 1. Install the Package

```bash
dotnet add package RandomSkunk.StructuredLogging
```

### 2. Start Logging

Use the extension methods on `Microsoft.Extensions.Logging.ILogger`.

#### Basic Logging with Properties

Pass properties as a list of `(string, object)` tuples. The message remains clean and readable.

```csharp
logger.Information("User logged in successfully",
    ("UserId", user.Id),
    ("SessionId", sessionId),
    ("LoginTime", DateTime.UtcNow));
```
*Output Log (conceptual JSON):*
```json
{
  "Message": "User logged in successfully",
  "UserId": 123,
  "SessionId": "xyz-abc",
  "LoginTime": "2024-01-01T12:00:00Z"
}
```

#### Property Extraction from Interpolated Strings

For ultimate convenience, extract properties directly from an interpolated string. The syntax `{value:<PropertyName>}` captures the value as a property and embeds it in the message.

This is not just a simple `string.Format`. The library uses a custom interpolated string handler that **only evaluates the arguments and formats the string if the log level is enabled**.

```csharp
// The values for username and attemptCount are captured as properties.
logger.Warning($"Failed login attempt for {username:<Username>}",
    ("AttemptCount", attemptCount),
    ("IPAddress", clientIp));
```
*Output Log (conceptual JSON):*
```json
{
  "Message": "Failed login attempt for brian",
  "Username": "brian",
  "AttemptCount": 3,
  "IPAddress": "127.0.0.1"
}
```

## Performance: Fast and Memory-Efficient

Performance is a core feature. This library is designed to minimize overhead in your application.

### Conditional Evaluation

The custom interpolated string handlers are the magic behind the performance. String formatting and method calls inside an interpolated string **only occur if the log level is enabled**.

```csharp
// If Debug logging is disabled, CalculateSize() is never called and no string is created.
logger.Debug($"Processing {items.Count:<ItemsCount>} items with total size {CalculateSize(items):<ItemsByteCount>N0} bytes");
```

### No Message Caching

Unlike other libraries, we **do not cache message templates**. This eliminates memory overhead and performance penalties associated with managing a cache, making it ideal for dynamic log messages.

## Advanced Usage

The library is flexible enough to handle any scenario.

### Logging with Exceptions and Event IDs

All overloads support standard `EventId` and `Exception` arguments.

```csharp
logger.Error(new EventId(500, "DatabaseError"), exception, "Database connection failed",
    ("ConnectionString", connectionString),
    ("RetryCount", retryCount));
```

### Using Dictionaries for Properties

You can pass properties in any `IReadOnlyCollection<KeyValuePair<string, object?>>`, including a `Dictionary`.

```csharp
var metadata = new Dictionary<string, object?>
{
    ["UserId"] = user.Id,
    ["TenantId"] = tenant.Id,
    ["CorrelationId"] = correlationId
};

logger.Information("Operation completed", metadata);
```

## How It Works

The library extends `ILogger` with a new set of extension methods. These methods use custom [interpolated string handlers](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/improved-interpolated-strings) to intercept string formatting.

1. The handler checks if the requested `LogLevel` is enabled.
2. If not, it does nothing, and the call is nearly free.
3. If enabled, it processes the interpolated string, extracting any properties defined with the `<Key>` syntax.
4. It then combines all properties and passes them, along with the formatted message, to the underlying `ILogger` instance.
5. The library uses an optimized array-based approach for passing properties.

## Compatibility

- **.NET 8.0**
- **.NET 9.0**
- **.NET 10.0**
- Compatible with all `Microsoft.Extensions.Logging` providers (OpenTelemetry, Serilog, Console, etc.)

## License

This project is licensed under the MIT License.

Copyright (c) 2025-2026 Brian Friesen. All rights reserved.
