# RandomSkunk.StructuredLogging

[![NuGet](https://img.shields.io/nuget/v/RandomSkunk.StructuredLogging.svg)](https://www.nuget.org/packages/RandomSkunk.StructuredLogging/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

RandomSkunk.StructuredLogging provides structured logging extensions for .NET, designed to separate human-readable messages from machine-readable properties. This approach helps keep your logs clear, maintainable, and easy to query, without mixing data into message templates.

## Why Choose RandomSkunk.StructuredLogging?

The default logging extension methods from Microsoft.Extensions.Logging force you to embed structured log properties into message
templates. This often leads to:

- **Verbose and Unreadable Messages**: `logger.LogInformation("User {UserId} logged in from {IPAddress}", userId, ipAddress)`
- **Performance Overhead**: Message template caching can consume memory and CPU.
- **Rigid Structure**: You can only log what your template allows.

This library takes a different approach by treating messages and properties as separate concerns, giving you the best of both
worlds: **clean, readable messages** and **rich, queryable data**.

## Features

- ✨ **Clean Separation**: Keep your log messages for humans and your properties for machines.
- 🚀 **High Performance**: A design that avoids message template caching overhead.
- 📝 **Powerful Interpolated Strings**: Automatically extract attributes from interpolated strings
  (`$"User {user.Name:<UserName>}"`) without sacrificing performance. The interpolation only happens if the log level is enabled!
- 💪 **Flexible & Type-Safe**: Pass properties using tuples, dictionaries, or arrays with a rich set of overloads.
- 🔄 **Operation Logging**: Track operation start/completion logs with shared context, per-operation trace entries, and optional
  return values/exceptions.

## Quick Start

### 1. Install the Package

```bash
dotnet add package RandomSkunk.StructuredLogging
```

### 2. Start Logging


Use the extension methods provided by RandomSkunk.StructuredLogging on `Microsoft.Extensions.Logging.ILogger`.

#### Basic Logging with Properties

Pass properties as a list of `(string, object?)` tuples. The message remains clean and readable, and properties are attached as structured data.

```csharp
logger.Information("User logged in successfully",
    ("UserId", user.Id),
    ("SessionId", sessionId),
    ("LoginTime", DateTime.UtcNow));
```

*Example output (conceptual JSON):*
```json
{
  "Message": "User logged in successfully",
  "UserId": 123,
  "SessionId": "xyz-abc",
  "LoginTime": "2024-01-01T12:00:00Z"
}
```


#### Property Extraction from Interpolated Strings

You can extract properties directly from an interpolated string using the syntax `{value:<PropertyName>}`. This captures the value as a property and embeds it in the message.

The library uses a custom interpolated string handler, so arguments and formatting only occur if the log level is enabled.

```csharp
// The values for username, attemptCount, and clientIp are captured as properties.
logger.Warning($"Failed login attempt for {username:<Username>}",
    ("AttemptCount", attemptCount),
    ("IPAddress", clientIp));
```

*Example output (conceptual JSON):*
```json
{
  "Message": "Failed login attempt for brian",
  "Username": "brian",
  "AttemptCount": 3,
  "IPAddress": "127.0.0.1"
}
```


#### Operation Logging

Use `LogOperation` to create an operation log that writes a single structured log entry when disposed. The returned `IOperationLog` instance provides methods to log values, conditions, and return values within the operation. This log summarizes the operation's context and any details you record during its execution.

```csharp
public int Divide(int dividend, int divisor, int? fallbackValue = null)
{
    using var log = logger.LogOperation(
        $"{typeof(Calculator)}.{nameof(Divide)}",
        ("Dividend", dividend),
        ("Divisor", divisor),
        ("FallbackValue", fallbackValue));

    if (!log.IsNull(fallbackValue) && log.Condition(divisor == 0))
    {
        log.Append($"Cannot divide by zero. Returning fallback value, {fallbackValue}.");
        return log.ReturnValue(fallbackValue.Value);
    }

    try
    {
        return log.ReturnValue(dividend / divisor);
    }
    catch (Exception ex)
    {
        logger.Error(log.EventId, log.Exception(ex), "Error performing division. Rethrowing exception...", log.Properties);
        throw;
    }
}
```

*Example output (conceptual JSON):*

```json
{
  "Message": "Operation complete: MathUtilities.Calculator.Divide",
  "Dividend": 10,
  "Divisor": 2,
  "FallbackValue": null,
  "Operation.ReturnValue": 5,
  "Operation.Log": "[12:34:56.785Z] Operation started
[12:34:56.786Z] `fallbackValue` is not null
[12:34:56.787Z] `divisor == 0` is false
[12:34:56.788Z] Return value set to `dividend / divisor`
[12:34:56.789Z] Operation complete"
}
```

## Performance: Fast and Memory-Efficient

Performance is a core feature. This library is designed to minimize overhead in your application.

### Conditional Evaluation

The custom interpolated string handlers are the magic behind the performance. String formatting and method calls inside an
interpolated string **only occur if the log level is enabled**.

```csharp
// If Debug logging is disabled, CalculateSize() is never called and no string is created.
logger.Debug($"Processing {items.Count:<ItemsCount>} items with total size {CalculateSize(items):<ItemsByteCount>N0} bytes");
```

### No Message Caching

Unlike other libraries, we **do not cache message templates**. This eliminates memory overhead and performance penalties associated
with managing a cache, making it ideal for dynamic log messages.

## Advanced Usage


### Additional Features

RandomSkunk.StructuredLogging supports a variety of advanced scenarios for structured logging.


#### Logging with Exceptions and Event IDs

All logging methods support `EventId` and `Exception` parameters, allowing you to attach additional context to your logs.

```csharp
logger.Error(new EventId(500, "DatabaseError"), exception, "Database connection failed",
    ("ConnectionString", connectionString),
    ("RetryCount", retryCount));
```


#### Using Dictionaries for Properties

You can pass properties as any `IReadOnlyCollection<KeyValuePair<string, object?>>`, such as a `Dictionary`:

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

The library extends `ILogger` with a new set of extension methods for structured event logs and operation logs. These methods
use custom [interpolated string handlers]
(https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/improved-interpolated-strings) to
intercept string formatting.

1. The handler checks if the requested `LogLevel` is enabled.
2. If not, it does nothing, and the call is nearly free.
3. If enabled, it processes the interpolated string, extracting any properties defined with the `<Key>` syntax.
4. It then combines all properties and passes them, along with the formatted message, to the underlying `ILogger` instance.
5. The library uses an optimized struct-based approach for passing properties.

## Compatibility


RandomSkunk.StructuredLogging targets:
- .NET 8.0
- .NET 9.0
- .NET 10.0

It is compatible with all `Microsoft.Extensions.Logging` providers, including OpenTelemetry, Serilog, and the built-in Console logger.

## License

This project is licensed under the MIT License.

---

## Contributing & Support

Contributions, issues, and suggestions are welcome. Please open an issue or pull request on GitHub if you have feedback or need help.
