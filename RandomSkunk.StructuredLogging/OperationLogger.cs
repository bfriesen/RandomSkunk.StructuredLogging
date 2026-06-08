using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging;

internal class OperationLogger<TCategoryName>(ILogger<TCategoryName>? logger = null)
    : IOperationLogger<TCategoryName>
{
    IOperationLog IOperationLogger.LogOperation(string operationName) =>
        logger.LogOperation(operationName);

    IOperationLog IOperationLogger.LogOperation(LogLevel logLevel, string operationName) =>
        logger.LogOperation(logLevel, operationName);

    IOperationLog IOperationLogger.LogOperation(EventId eventId, string operationName) =>
        logger.LogOperation(eventId, operationName);

    IOperationLog IOperationLogger.LogOperation(LogLevel logLevel, EventId eventId, string operationName) =>
        logger.LogOperation(logLevel, eventId, operationName);
}
