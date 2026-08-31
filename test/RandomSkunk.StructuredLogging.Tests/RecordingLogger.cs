using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

/// <summary>
/// A minimal <see cref="ILogger"/> test double that records the arguments of the last <see cref="Log"/> call,
/// so tests can assert on what the extension methods actually passed through. Not sealed so
/// <see cref="RecordingLogger{T}"/> can add the <see cref="ILogger{TCategoryName}"/> marker without
/// duplicating any of this.
/// </summary>
internal class RecordingLogger : ILogger
{
    public bool Enabled { get; set; } = true;

    public IDisposable? ScopeToReturn { get; set; }

    public object? LastScopeState { get; private set; }

    public int LogCallCount { get; private set; }

    public LogLevel LastLevel { get; private set; }

    public EventId LastEventId { get; private set; }

    public Exception? LastException { get; private set; }

    public string? LastMessage { get; private set; }

    public IReadOnlyList<KeyValuePair<string, object?>>? LastProperties { get; private set; }

    /// <summary>
    /// When set, <see cref="Log"/> records the call and then throws this exception - a stand-in for a
    /// misbehaving sink, so tests can assert what the library does when the write itself fails.
    /// </summary>
    public Exception? ThrowOnLog { get; set; }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        LastScopeState = state;
        return ScopeToReturn;
    }

    public bool IsEnabled(LogLevel logLevel) => Enabled;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogCallCount++;
        LastLevel = logLevel;
        LastEventId = eventId;
        LastException = exception;
        LastMessage = formatter(state, exception);
        LastProperties = state as IReadOnlyList<KeyValuePair<string, object?>>;

        if (ThrowOnLog is not null)
            throw ThrowOnLog;
    }
}

/// <summary>
/// The <see cref="ILogger{TCategoryName}"/> counterpart to <see cref="RecordingLogger"/>, for testing code
/// that depends on <see cref="RandomSkunk.StructuredLogging.Operation.OperationLogFactory{TCategoryName}"/>.
/// <see cref="ILogger{TCategoryName}"/> adds no members beyond <see cref="ILogger"/>, so there's nothing to
/// override here.
/// </summary>
/// <typeparam name="T">The logger's category type.</typeparam>
internal sealed class RecordingLogger<T> : RecordingLogger, ILogger<T>
{
}
