using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Begins operation logs against a captured <see cref="ILogger"/>, mirroring
/// <see cref="LoggerOperationExtensions"/>'s <c>BeginOperation</c> overloads as instance methods instead of
/// extension methods on <see cref="ILogger"/> directly.
/// <para>
/// A class that depends on this instead of calling <c>logger.BeginOperation(...)</c> itself gains a seam:
/// production code registers the real logger behind it, while a test substitutes a mock (or subclass) of
/// this type - <see cref="BeginOperation(string, LogLevel, bool)"/> and its <see cref="EventId"/> overload
/// are both <see langword="virtual"/> for exactly that purpose - returning a <see cref="FakeOperationLog"/>
/// in place of a real operation. That avoids the alternative of asserting against the raw <see cref="ILogger"/>
/// call the operation eventually makes, which requires knowing the library's internal journal-text format
/// and structured-property names - see the README's "Testing code that takes an <see cref="IOperationLog"/>"
/// section.
/// </para>
/// <para>
/// <see cref="OperationLogFactory{TCategoryName}"/> is the generic counterpart, closed over the type that
/// depends on it exactly as <see cref="ILogger{TCategoryName}"/> already is - register it once, as an open
/// generic, and a DI container resolves an <see cref="OperationLogFactory{TCategoryName}"/> for any
/// consuming type with no per-type registration of your own.
/// </para>
/// </summary>
public class OperationLogFactory
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationLogFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger every operation this factory begins will eventually write its
    /// single log entry to.</param>
    public OperationLogFactory(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Begins an operation on the logger captured by this factory. See
    /// <see cref="LoggerOperationExtensions.BeginOperation(ILogger, string, LogLevel, bool)"/> for the full
    /// behavior this delegates to.
    /// </summary>
    /// <param name="operationName">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">
    /// The severity level the operation's final log entry is written at. Defaults to
    /// <see cref="LogLevel.Information"/>.
    /// </param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public virtual IOperationLog BeginOperation(string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        _logger.BeginOperation(operationName, level, threadSafe);

    /// <summary>
    /// Begins an operation, tagged with <paramref name="eventId"/>, on the logger captured by this factory.
    /// See <see cref="LoggerOperationExtensions.BeginOperation(ILogger, EventId, string, LogLevel, bool)"/>
    /// for the full behavior this delegates to.
    /// </summary>
    /// <param name="eventId">The event id associated with the operation's final log entry.</param>
    /// <param name="operationName">The operation's name, used in its journal lines and its final log message.</param>
    /// <param name="level">
    /// The severity level the operation's final log entry is written at. Defaults to
    /// <see cref="LogLevel.Information"/>.
    /// </param>
    /// <param name="threadSafe">
    /// <see langword="true"/> to make the returned <see cref="IOperationLog"/> (and every
    /// sub-operation begun from it) safe to use concurrently, e.g. from sub-operations run
    /// via <c>Task.WhenAll</c>. By default an operation applies no synchronization of its own.
    /// </param>
    /// <returns>An <see cref="IOperationLog"/> representing the operation.</returns>
    public virtual IOperationLog BeginOperation(EventId eventId, string operationName, LogLevel level = LogLevel.Information, bool threadSafe = false) =>
        _logger.BeginOperation(eventId, operationName, level, threadSafe);
}

/// <summary>
/// The generic-category-name counterpart to <see cref="OperationLogFactory"/>, mirroring
/// <see cref="ILogger{TCategoryName}"/>: a class depends on this closed over itself
/// (<c>OperationLogFactory&lt;OrderShipper&gt;</c>, exactly as it would depend on
/// <c>ILogger&lt;OrderShipper&gt;</c>), and a DI container that already resolves
/// <see cref="ILogger{TCategoryName}"/> - every container built on
/// <c>Microsoft.Extensions.DependencyInjection</c> does, once logging is registered - resolves this too,
/// once <see cref="OperationLogFactory{TCategoryName}"/> itself is registered as an open generic
/// (<c>services.AddSingleton(typeof(OperationLogFactory&lt;&gt;))</c>), with no per-type registration
/// beyond that.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the <see cref="ILogger{TCategoryName}"/>
/// category.</typeparam>
public class OperationLogFactory<TCategoryName> : OperationLogFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationLogFactory{TCategoryName}"/> class.
    /// </summary>
    /// <param name="logger">The category-typed logger every operation this factory begins will eventually
    /// write its single log entry to.</param>
    public OperationLogFactory(ILogger<TCategoryName> logger)
        : base(logger)
    {
    }
}
