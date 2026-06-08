using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines an object that creates operation logs, which write their contents when disposed.
/// </summary>
public interface IOperationLogger
{
    /// <summary>
    /// Creates an operation log that writes its contents at the <see cref="LogLevel.Debug"/> level when disposed.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>An <see cref="IOperationLog"/> object that writes its contents when disposed.</returns>
    IOperationLog LogOperation(string operationName);

    /// <summary>
    /// Creates an operation log that writes its contents when disposed.
    /// </summary>
    /// <param name="logLevel">The level at which the operation log is written.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>An <see cref="IOperationLog"/> object that writes its contents when disposed.</returns>
    IOperationLog LogOperation(LogLevel logLevel, string operationName);

    /// <summary>
    /// Creates an operation log that writes its contents at the <see cref="LogLevel.Debug"/> level when disposed.
    /// </summary>
    /// <param name="eventId">The <see cref="EventId"/> associated with the operation log.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>An <see cref="IOperationLog"/> object that writes its contents when disposed.</returns>
    IOperationLog LogOperation(EventId eventId, string operationName);

    /// <summary>
    /// Creates an operation log that writes its contents when disposed.
    /// </summary>
    /// <param name="logLevel">The level at which the operation log is written.</param>
    /// <param name="eventId">The <see cref="EventId"/> associated with the operation log.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <returns>An <see cref="IOperationLog"/> object that writes its contents when disposed.</returns>
    IOperationLog LogOperation(LogLevel logLevel, EventId eventId, string operationName);
}

/// <summary>
/// Defines an object that creates operation logs that write their contents when disposed.
/// </summary>
/// <typeparam name="TCategoryName">The type whose name is used for the logger category name.</typeparam>
public interface IOperationLogger<TCategoryName> : IOperationLogger
{
}
