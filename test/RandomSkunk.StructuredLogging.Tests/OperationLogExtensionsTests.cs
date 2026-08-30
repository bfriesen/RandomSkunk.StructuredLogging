using AwesomeAssertions;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLogExtensionsTests
{
    [Fact]
    public void SetResultTo_ReturnsValueUnchangedAndSetsOperationResult()
    {
        RecordingLogger logger = new();

        string result;
        using (IOperationLog log = logger.BeginOperation("Name"))
            result = "shipped".SetResultTo(log);

        result.Should().Be("shipped");
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "shipped"));
    }

    [Fact]
    public void SetResultTo_NullLog_ThrowsArgumentNullException()
    {
        Func<string> act = () => "value".SetResultTo(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AppendResultTo_ReturnsValueUnchangedAndAppendsJournalLine()
    {
        RecordingLogger logger = new();

        string result;
        using (IOperationLog log = logger.BeginOperation("Name"))
            result = "shipped".AppendResultTo(log);

        result.Should().Be("shipped");
        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation result: shipped");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void AppendResultTo_SubOperation_AppendsJournalLineUnderSubOperationName()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            "done".AppendResultTo(subLog);
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch` result: done");
    }

    [Fact]
    public void AppendResultTo_NullLog_ThrowsArgumentNullException()
    {
        Func<string> act = () => "value".AppendResultTo<string, IOperationLog>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPropertyTo_ReturnsValueUnchangedAndSetsProperty()
    {
        RecordingLogger logger = new();

        int orderId;
        using (IOperationLog log = logger.BeginOperation("Name"))
            orderId = 42.AddPropertyTo(log, "OrderId");

        orderId.Should().Be(42);
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("OrderId", 42));
    }

    [Fact]
    public void AddPropertyTo_NullLog_ThrowsArgumentNullException()
    {
        Func<string> act = () => "value".AddPropertyTo<string, IOperationLog>(null!, "PropertyName");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AppendValueTo_ReturnsValueUnchangedAndAppendsJournalLine()
    {
        RecordingLogger logger = new();

        int value;
        using (IOperationLog log = logger.BeginOperation("Name"))
            value = 7.AppendValueTo(log, "Count");

        value.Should().Be(7);
        string journal = logger.LastMessage!;
        journal.Should().Contain("`Count`: 7");
    }

    [Fact]
    public void AppendValueTo_DefaultName_UsesCallerArgumentExpression()
    {
        RecordingLogger logger = new();
        var order = new { Total = 42.5m };

        using (IOperationLog log = logger.BeginOperation("Name"))
            order.Total.AppendValueTo(log);

        string journal = logger.LastMessage!;
        journal.Should().Contain("`order.Total`: 42.5");
    }

    [Fact]
    public void AppendValueTo_NullLog_ThrowsArgumentNullException()
    {
        Func<int> act = () => 1.AppendValueTo<int, IOperationLog>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AppendJsonTo_ReturnsValueUnchangedAndAppendsJsonJournalLine()
    {
        RecordingLogger logger = new();

        object value;
        using (IOperationLog log = logger.BeginOperation("Name"))
            value = new { A = 1 }.AppendJsonTo(log, "Payload");

        value.Should().BeEquivalentTo(new { A = 1 });
        string journal = logger.LastMessage!;
        journal.Should().Contain("`Payload`:");
        journal.Should().Contain("\"A\": 1");
    }

    [Fact]
    public void AppendJsonTo_NullLog_ThrowsArgumentNullException()
    {
        var act = () => new { A = 1 }.AppendJsonTo<object, IOperationLog>(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
