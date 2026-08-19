using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// An <see cref="IOperationLogger{TCategoryName}"/> base class for test code to derive from and override,
/// to verify operation-logging behavior without a real <see cref="ILogger{TCategoryName}"/>. Its
/// <c>BeginOperation</c> overloads are <see langword="virtual"/>; by default, the <see cref="EventId"/>
/// overload returns a <see cref="DisabledOperationLog"/> (rather than forwarding to a real logger, the
/// way <see cref="OperationLogger{TCategoryName}"/> does) and the other overload just calls it, so a base
/// <see cref="TestOperationLogger{TCategoryName}"/> used without overriding either method never appends to
/// the injected <see cref="ILogger{TCategoryName}"/>. Its other members (<see cref="Log"/>,
/// <see cref="IsEnabled"/>, <see cref="BeginScope"/>) still just forward to the injected
/// <see cref="ILogger{TCategoryName}"/>. To use this: give the class under test a second constructor
/// overload that accepts <see cref="IOperationLogger{TCategoryName}"/> in place of
/// <see cref="ILogger{TCategoryName}"/> (having the existing constructor call it via
/// <c>logger.ToOperationLogger()</c>), store it in a field typed as
/// <see cref="IOperationLogger{TCategoryName}"/> instead of <see cref="ILogger{TCategoryName}"/>, and pass
/// a subclass of this type - overriding <c>BeginOperation</c> to return a test double - in tests.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the log category.</typeparam>
/// <param name="logger">The logger <see cref="Log"/>, <see cref="IsEnabled"/>, and <see cref="BeginScope"/> forward to.</param>
public class TestOperationLogger<TCategoryName>(ILogger<TCategoryName> logger) : IOperationLogger<TCategoryName>
{
    /// <inheritdoc/>
    public virtual IOperationLog BeginOperation(string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        BeginOperation(default, name, level, threadSafe);

    /// <inheritdoc/>
    public virtual IOperationLog BeginOperation(EventId eventId, string name, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        new DisabledOperationLog(eventId);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        logger.BeginScope(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) =>
        logger.IsEnabled(logLevel);

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        logger.Log(logLevel, eventId, state, exception, formatter);
}
