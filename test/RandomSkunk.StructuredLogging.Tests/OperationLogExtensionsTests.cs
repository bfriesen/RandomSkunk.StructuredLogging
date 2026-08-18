using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLogExtensionsTests
{
    [Fact]
    public void RecordResultTo_ReturnsValueUnchangedAndSetsOperationResult()
    {
        var logger = new RecordingLogger();

        string result;
        using (var operation = logger.BeginOperation("Name"))
            result = "shipped".RecordResultTo(operation);

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
    public void RecordValueTo_ReturnsValueUnchangedAndAppendsJournalLine()
    {
        var logger = new RecordingLogger();

        int value;
        using (var operation = logger.BeginOperation("Name"))
            value = 7.RecordValueTo(operation, "Count");

        value.Should().Be(7);
        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Count`: 7");
    }

    [Fact]
    public void RecordValueTo_DefaultName_UsesCallerArgumentExpression()
    {
        var logger = new RecordingLogger();
        var order = new { Total = 42.5m };

        using (var operation = logger.BeginOperation("Name"))
            order.Total.RecordValueTo(operation);

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`order.Total`: 42.5");
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
        using (var operation = logger.BeginOperation("Name"))
            value = new { A = 1 }.RecordJsonTo(operation, "Payload");

        value.Should().BeEquivalentTo(new { A = 1 });
        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Payload`:");
        log.Should().Contain("\"A\": 1");
    }

    [Fact]
    public void RecordJsonTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => new { A = 1 }.RecordJsonTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
