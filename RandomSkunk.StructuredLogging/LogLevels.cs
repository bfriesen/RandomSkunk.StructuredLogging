using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines a type that represents a log level.
/// </summary>
public interface ILogLevel
{
    /// <summary>
    /// Gets the log level.
    /// </summary>
    public LogLevel LogLevel { get; }
}

/// <summary>
/// Represents the trace log level.
/// </summary>
public readonly struct Trace : ILogLevel
{
    /// <summary>
    /// Gets the trace log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Trace;
}

/// <summary>
/// Represents the debug log level.
/// </summary>
public readonly struct Debug : ILogLevel
{
    /// <summary>
    /// Gets the debug log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Debug;
}

/// <summary>
/// Represents the information log level.
/// </summary>
public readonly struct Information : ILogLevel
{
    /// <summary>
    /// Gets the information log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Information;
}

/// <summary>
/// Represents the warning log level.
/// </summary>
public readonly struct Warning : ILogLevel
{
    /// <summary>
    /// Gets the warning log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Warning;
}

/// <summary>
/// Represents the error log level.
/// </summary>
public readonly struct Error : ILogLevel
{
    /// <summary>
    /// Gets the error log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Error;
}

/// <summary>
/// Represents the critical log level.
/// </summary>
public readonly struct Critical : ILogLevel
{
    /// <summary>
    /// Gets the critical log level.
    /// </summary>
    public LogLevel LogLevel => LogLevel.Critical;
}
