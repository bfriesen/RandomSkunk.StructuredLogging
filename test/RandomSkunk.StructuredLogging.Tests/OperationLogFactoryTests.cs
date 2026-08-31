using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLogFactoryTests
{
    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Action act = () => new OperationLogFactory(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void BeginOperation_DelegatesToTheCapturedLogger()
    {
        RecordingLogger logger = new();
        OperationLogFactory factory = new(logger);

        using (factory.BeginOperation("DoThing"))
        {
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastLevel.Should().Be(LogLevel.Information);
        logger.LastMessage.Should().StartWith("Operation: DoThing\n");
    }

    [Fact]
    public void BeginOperation_WithEventId_DelegatesToTheCapturedLogger()
    {
        RecordingLogger logger = new();
        OperationLogFactory factory = new(logger);
        EventId eventId = new(42, "Custom");

        using (factory.BeginOperation(eventId, "DoThing"))
        {
        }

        logger.LastEventId.Should().Be(eventId);
    }

    [Fact]
    public void BeginOperation_IsVirtual_SoATestCanSubstituteAFakeOperationLog()
    {
        // The whole reason this type exists: a class depending on OperationLogFactory instead of calling
        // ILogger.BeginOperation directly can be tested by mocking BeginOperation to return a
        // FakeOperationLog, with no knowledge of the library's journal format or structured property names.
        Mock<OperationLogFactory> factory = new(NullLogger.Instance);
        Mock<FakeOperationLog> log = new();
        factory.Setup(x => x.BeginOperation("ShipOrder", It.IsAny<LogLevel>(), It.IsAny<bool>())).Returns(log.Object);

        using (IOperationLog result = factory.Object.BeginOperation("ShipOrder"))
            result.Should().BeSameAs(log.Object);

        log.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void GenericFactory_ConstructorNullLogger_Throws()
    {
        Action act = () => new OperationLogFactory<OperationLogFactoryTests>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GenericFactory_UsesTheCategoryTypedLogger()
    {
        RecordingLogger<OperationLogFactoryTests> logger = new();
        OperationLogFactory<OperationLogFactoryTests> factory = new(logger);

        using (factory.BeginOperation("DoThing"))
        {
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastMessage.Should().StartWith("Operation: DoThing\n");
    }
}
