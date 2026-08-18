using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLogExtensionsTests
{
    [Fact]
    public void RecordResultTo_ReturnsValueUnchangedAndSetsOperationResult()
    {
        var logger = new RecordingLogger();

        string result;
        using (var log = logger.BeginOperation("Name"))
            result = "shipped".RecordResultTo(log);

        result.Should().Be("shipped");
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "shipped"));
    }

    [Fact]
    public void RecordResultTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => "value".RecordResultTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordPropertyTo_ReturnsValueUnchangedAndSetsProperty()
    {
        var logger = new RecordingLogger();

        int orderId;
        using (var log = logger.BeginOperation("Name"))
            orderId = 42.RecordPropertyTo(log, "OrderId");

        orderId.Should().Be(42);
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("OrderId", 42));
    }

    [Fact]
    public void RecordPropertyTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => "value".RecordPropertyTo(null!, "PropertyName");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordValueTo_ReturnsValueUnchangedAndAppendsJournalLine()
    {
        var logger = new RecordingLogger();

        int value;
        using (var log = logger.BeginOperation("Name"))
            value = 7.RecordValueTo(log, "Count");

        value.Should().Be(7);
        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Count`: 7");
    }

    [Fact]
    public void RecordValueTo_DefaultName_UsesCallerArgumentExpression()
    {
        var logger = new RecordingLogger();
        var order = new { Total = 42.5m };

        using (var log = logger.BeginOperation("Name"))
            order.Total.RecordValueTo(log);

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`order.Total`: 42.5");
    }

    [Fact]
    public void RecordValueTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => 1.RecordValueTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordJsonTo_ReturnsValueUnchangedAndAppendsJsonJournalLine()
    {
        var logger = new RecordingLogger();

        object value;
        using (var log = logger.BeginOperation("Name"))
            value = new { A = 1 }.RecordJsonTo(log, "Payload");

        value.Should().BeEquivalentTo(new { A = 1 });
        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
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
