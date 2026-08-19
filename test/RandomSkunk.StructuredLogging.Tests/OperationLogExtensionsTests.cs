using AwesomeAssertions;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLogExtensionsTests
{
    [Fact]
    public void RecordResultTo_ReturnsValueUnchangedAndSetsOperationResult()
    {
        RecordingLogger logger = new();

        string result;
        using (IOperationLog log = logger.BeginOperation("Name"))
            result = "shipped".RecordResultTo(log);

        result.Should().Be("shipped");
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "shipped"));
    }

    [Fact]
    public void RecordResultTo_NullLog_ThrowsArgumentNullException()
    {
        Func<string> act = () => "value".RecordResultTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordPropertyTo_ReturnsValueUnchangedAndSetsProperty()
    {
        RecordingLogger logger = new();

        int orderId;
        using (IOperationLog log = logger.BeginOperation("Name"))
            orderId = 42.RecordPropertyTo(log, "OrderId");

        orderId.Should().Be(42);
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("OrderId", 42));
    }

    [Fact]
    public void RecordPropertyTo_NullLog_ThrowsArgumentNullException()
    {
        Func<string> act = () => "value".RecordPropertyTo(null!, "PropertyName");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordValueTo_ReturnsValueUnchangedAndAppendsJournalLine()
    {
        RecordingLogger logger = new();

        int value;
        using (IOperationLog log = logger.BeginOperation("Name"))
            value = 7.RecordValueTo(log, "Count");

        value.Should().Be(7);
        string journal = logger.LastMessage!;
        journal.Should().Contain("`Count`: 7");
    }

    [Fact]
    public void RecordValueTo_DefaultName_UsesCallerArgumentExpression()
    {
        RecordingLogger logger = new();
        var order = new { Total = 42.5m };

        using (IOperationLog log = logger.BeginOperation("Name"))
            order.Total.RecordValueTo(log);

        string journal = logger.LastMessage!;
        journal.Should().Contain("`order.Total`: 42.5");
    }

    [Fact]
    public void RecordValueTo_NullLog_ThrowsArgumentNullException()
    {
        Func<int> act = () => 1.RecordValueTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordJsonTo_ReturnsValueUnchangedAndAppendsJsonJournalLine()
    {
        RecordingLogger logger = new();

        object value;
        using (IOperationLog log = logger.BeginOperation("Name"))
            value = new { A = 1 }.RecordJsonTo(log, "Payload");

        value.Should().BeEquivalentTo(new { A = 1 });
        string journal = logger.LastMessage!;
        journal.Should().Contain("`Payload`:");
        journal.Should().Contain("\"A\": 1");
    }

    [Fact]
    public void RecordJsonTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => new { A = 1 }.RecordJsonTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
