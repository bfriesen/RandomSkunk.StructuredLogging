using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

/// <summary>
/// A minimal <see cref="ILogger"/> test double that records the arguments of the last <see cref="Log"/> call,
/// so tests can assert on what the extension methods actually passed through.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    public bool Enabled { get; set; } = true;

    public int LogCallCount { get; private set; }

    public LogLevel LastLevel { get; private set; }

    public EventId LastEventId { get; private set; }

    public Exception? LastException { get; private set; }

    public string? LastMessage { get; private set; }

    public IReadOnlyList<KeyValuePair<string, object?>>? LastProperties { get; private set; }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => Enabled;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogCallCount++;
        LastLevel = logLevel;
        LastEventId = eventId;
        LastException = exception;
        LastMessage = formatter(state, exception);
        LastProperties = state as IReadOnlyList<KeyValuePair<string, object?>>;
    }
}
