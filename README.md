# RandomSkunk.StructuredLogging

Modern structured logging extensions for .NET that separate human-readable messages from machine-readable attributes. Include 
contextual data (user IDs, correlation IDs, metrics) in logs without forcing them into message templates.

## Features

- **Clean Separation**: Human-readable messages separate from structured data attributes
- **High Performance**: Zero caching overhead - any string can be used as a log message
- **Custom Interpolated Strings**: Conditional evaluation based on log level enablement and attribute extraction
- **Flexible Overloads**: Up to 8 strongly-typed attributes, arrays, or collections
- **Multi-Target**: Supports .NET 8, .NET 9 with optimized implementations

## Quick Start

```csharp
using Microsoft.Extensions.Logging;
using RandomSkunk.StructuredLogging;

// Basic logging with attributes (tuple syntax)
logger.Information("User logged in",
    ("UserId", userId),
    ("SessionId", sessionId),
    ("LoginTime", DateTime.UtcNow));

// Using interpolated strings with attribute extraction
logger.Warning($"Failed login attempt for {username:<Username>}",
    ("AttemptCount", attemptCount),
    ("IPAddress", clientIp));

// With exception and event ID
logger.Error(eventId, exception, "Database connection failed",
    ("ConnectionString", connectionString),
    ("RetryCount", retryCount));
```

## Attribute Extraction in Interpolated Strings

You can extract log attributes directly from interpolated strings using the `<Key/>` format:

```csharp
logger.Information($"Order {orderId:<OrderId>} processed for {user.Name:<UserName>}");
```
- The value inside `{...:<Key>}` is added as a structured attribute with the specified key.
- You can combine this with additional attributes passed as tuples or collections.

## Performance Benefits

### No String Caching

Unlike traditional structured logging approaches, RandomSkunk.StructuredLogging **does not cache strings**. This means:

- Any string can be used as a log message without performance penalties
- No memory overhead from string caching mechanisms
- Perfect for dynamic messages that change frequently

```csharp
// These are all equally performant - no caching overhead
logger.Information($"Processing order {orderId} at {DateTime.Now}");
logger.Information($"User {user.Name} performed action at {timestamp:yyyy-MM-dd}");
logger.Information(GetDynamicMessage(context)); // Any string source works
```

### Best Practice for String Construction

If you're constructing expensive strings for logging (especially for lower-level logs), be sure to check if logging is enabled
first:

```csharp
if (logger.IsEnabled(LogLevel.Debug))
{
    string expensiveMessage = BuildComplexDiagnosticMessage(data);
    logger.Debug(expensiveMessage, ("RequestId", requestId));
}
```

Alternatively, you can inline the check by calling the method in an interpolated string:

```csharp
logger.Debug($"{BuildComplexDiagnosticMessage(data)}", ("RequestId", requestId));
```

### Custom Interpolated String Handlers

The library provides custom interpolated string handlers that only evaluate when logging is enabled at the specified level and extract attributes:

```csharp
// String interpolation and attribute extraction are conditional
logger.Debug($"Processing {items.Count:<ItemsCount>} items with total size {CalculateSize(items):<ItemsByteCount>N0} bytes");

// If Debug logging is disabled, the interpolation never occurs
// No performance impact from string formatting or method calls
```

## Advanced Usage

### Dictionary and Collection Support

Use any type implementing `IReadOnlyCollection<KeyValuePair<string, object?>>`:

```csharp
if (logger.IsEnabled(LogLevel.Information))
{
    Dictionary<string, object?> metadata = new()
    {
        ["UserId"] = user.Id,
        ["TenantId"] = tenant.Id,
        ["CorrelationId"] = correlationId
    };

    logger.Information("Operation completed", metadata);
}
```

### Multiple Overloads

The library provides extensive overloads for different scenarios:

```csharp
// Message only
logger.Information("Simple message");

// With up to 8 strongly-typed attributes
logger.Information("Message", ("Key1", value1), ("Key2", value2), ...);

// With attribute array
logger.Information("Message", new[] { ("Key", value), ... });

// With exception
logger.Error(exception, "Operation failed", ("OperationId", opId));

// With event ID and exception
logger.Critical(eventId, exception, "System failure", 
    ("ComponentId", componentId),
    ("ErrorCode", errorCode));

// With attribute collection
logger.Warning("Something happened", metadata);
```

## Architecture

### Conditional Evaluation

The custom interpolated string handlers ensure string formatting and attribute extraction only occur when logging is enabled:

```csharp
// Used in the Write extension methods.
public ref struct WriteInterpolatedStringHandler { /* ... */ }

// Used in the Trace, Debug, Information, Warning, Error, and Critical extension methods.
public ref struct InformationInterpolatedStringHandler { /* ... */ }
// ... and similar for other log levels
```

### Optimized for Different Runtimes

- **.NET 9+**: Uses `Span<T>` for zero-allocation attribute passing
- **.NET 8**: Uses arrays with optimized copying strategies
- **Source Generation**: Compile-time generation of extension methods for optimal performance

## Installation

```bash
dotnet add package RandomSkunk.StructuredLogging
```

## Compatibility

- **.NET 8.0** and later
- **.NET 9.0** with enhanced performance optimizations
- Compatible with all `Microsoft.Extensions.Logging` providers

## License

MIT License.
Copyright 2025 (c) Brian Friesen. All rights reserved.
