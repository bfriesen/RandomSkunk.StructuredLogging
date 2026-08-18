using AwesomeAssertions;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLoggingTests
{
    [Fact]
    public void Disabled_ReturnsNoOpAndDoesNotLog()
    {
        var logger = new RecordingLogger { Enabled = false };

        using (var operation = logger.BeginOperation("Test"))
        {
            operation
                .SetProperty("x", 1)
                .Append("hi")
                .AppendValue(5)
                .AppendJson(new { A = 1 })
                .SetResult(1)
                .SetException(new InvalidOperationException());

            using var sub = operation.BeginSubOperation("Sub");
            sub.SetProperty("y", 2).SetResult(3).SetException(new InvalidOperationException());
        }

        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Enabled_WritesExactlyOneLogEntryOnDispose()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("DoThing"))
        {
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastLevel.Should().Be(LogLevel.Information);
        logger.LastMessage.Should().Be("Operation complete: DoThing");
        logger.LastEventId.Should().Be(default(EventId));
        logger.LastException.Should().BeNull();
    }

    [Fact]
    public void WithEventId_UsesEventIdOnFinalEntry()
    {
        var logger = new RecordingLogger();
        var eventId = new EventId(42, "Custom");

        using (logger.BeginOperation(eventId, "Name"))
        {
        }

        logger.LastEventId.Should().Be(eventId);
    }

    [Fact]
    public void WithLevel_UsesLevelOnFinalEntry()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("Name", LogLevel.Warning))
        {
        }

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void FinalEntry_IncludesBuiltInOperationProperties()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("Name"))
        {
        }

        var properties = logger.LastProperties!.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        properties.Should().ContainKey("Operation.StartTime").WhoseValue.Should().BeOfType<DateTimeOffset>();
        properties.Should().ContainKey("Operation.DurationMs").WhoseValue.Should().BeOfType<double>();
        properties.Should().ContainKey("Operation.Log").WhoseValue.Should().BeOfType<string>();
        properties.Should().NotContainKey("Operation.Result");
    }

    [Fact]
    public void Journal_StartsWithOperationStartedLine()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("Name"))
        {
        }

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;

        log.Should().Contain("Operation started.");
    }

    [Fact]
    public void SetProperty_AddsUnprefixedStructuredProperty()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
            operation.SetProperty("UserId", 123);

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void SetResult_SetsOperationResultProperty()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
            operation.SetResult("done");

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "done"));
    }

    [Fact]
    public void SetException_SetsExceptionOnFinalEntry()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var operation = logger.BeginOperation("Name"))
            operation.SetException(exception);

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void Append_AddsLineToOperationLog()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
            operation.Append("custom line");

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("custom line");
    }

    [Fact]
    public void AppendValue_UsesCallerArgumentExpressionForDefaultName()
    {
        var logger = new RecordingLogger();
        var total = 42.5m;

        using (var operation = logger.BeginOperation("Name"))
            operation.AppendValue(total);

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`total`: 42.5");
    }

    [Fact]
    public void AppendValue_ExplicitName_OverridesDefault()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
            operation.AppendValue(42, "Count");

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Count`: 42");
    }

    [Fact]
    public void AppendJson_RendersIndentedJsonUnderGivenName()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
            operation.AppendJson(new { A = 1, B = "x" }, "Payload");

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Payload`:");
        log.Should().Contain("\"A\": 1");
        log.Should().Contain("\"B\": \"x\"");
    }

    [Fact]
    public void BeginSubOperation_AppendsStartedAndCompleteLines()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
        {
            using var sub = operation.BeginSubOperation("Fetch");
        }

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Fetch` started.");
        log.Should().Contain("`Fetch` complete.");
    }

    [Fact]
    public void SubOperation_SetProperty_AddsToRootFinalEntry()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
        {
            using var sub = operation.BeginSubOperation("Fetch");
            sub.SetProperty("Count", 5);
        }

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void SubOperation_SetResult_AppendsJournalLine_NotRootResultProperty()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
        {
            using var sub = operation.BeginSubOperation("Fetch");
            sub.SetResult(99);
        }

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Fetch` result: 99");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void SubOperation_SetException_DefaultDoesNotPropagateToRoot()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var operation = logger.BeginOperation("Name"))
        {
            using var sub = operation.BeginSubOperation("Fetch");
            sub.SetException(exception);
        }

        logger.LastException.Should().BeNull();

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Fetch` failed:");
        log.Should().Contain(exception.ToString());
    }

    [Fact]
    public void SubOperation_SetException_PropagateToRootTrue_SetsRootException()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var operation = logger.BeginOperation("Name"))
        {
            using var sub = operation.BeginSubOperation("Fetch");
            sub.SetException(exception, propagateToRoot: true);
        }

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void NestedSubOperations_AllContributeToSameJournal()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
        {
            using var outer = operation.BeginSubOperation("Outer");
            using var inner = outer.BeginSubOperation("Inner");
        }

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        log.Should().Contain("`Outer` started.");
        log.Should().Contain("`Inner` started.");
        log.Should().Contain("`Inner` complete.");
        log.Should().Contain("`Outer` complete.");
    }

    [Fact]
    public void RootDispose_IsIdempotent()
    {
        var logger = new RecordingLogger();

        var operation = logger.BeginOperation("Name");
        operation.Dispose();
        operation.Dispose();

        logger.LogCallCount.Should().Be(1);
    }

    [Fact]
    public void ChildDispose_IsIdempotent()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name"))
        {
            var sub = operation.BeginSubOperation("Fetch");
            sub.Dispose();
            sub.Dispose();
        }

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        CountOccurrences(log, "`Fetch` complete.").Should().Be(1);
    }

    [Fact]
    public async Task ThreadSafe_AllowsConcurrentSubOperations()
    {
        var logger = new RecordingLogger();

        using (var operation = logger.BeginOperation("Name", threadSafe: true))
        {
            var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                using var sub = operation.BeginSubOperation($"Sub{i}");
                sub.SetProperty($"P{i}", i);
                sub.AppendValue(i, "value");
            }));

            await Task.WhenAll(tasks);
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastProperties!.Count(kvp => kvp.Key.StartsWith('P')).Should().Be(20);

        var log = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Log").Value!;
        for (var i = 0; i < 20; i++)
        {
            log.Should().Contain($"`Sub{i}` started.");
            log.Should().Contain($"`Sub{i}` complete.");
        }
    }

    [Fact]
    public void BeginOperation_NullLogger_ThrowsArgumentNullException()
    {
        ILogger logger = null!;

        var act1 = () => logger.BeginOperation("Name");
        var act2 = () => logger.BeginOperation(default, "Name");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
