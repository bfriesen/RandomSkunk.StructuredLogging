using System.Globalization;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

public class OperationLoggingTests
{
    [Fact]
    public void Disabled_ReturnsNoOpAndDoesNotLog()
    {
        var logger = new RecordingLogger { Enabled = false };

        using (var log = logger.BeginOperation("Test"))
        {
            log
                .AddProperty("x", 1)
                .Append("hi")
                .AppendValue(5)
                .AppendJson(new { A = 1 })
                .SetResult(1)
                .SetException(new InvalidOperationException());

            using var subLog = log.BeginSubOperation("Sub");
            subLog.AddProperty("y", 2).SetResult(3).SetException(new InvalidOperationException());
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

        properties.Should().ContainKey("Operation.Name").WhoseValue.Should().Be("Name");
        properties.Should().ContainKey("Operation.StartTime").WhoseValue.Should().BeOfType<DateTimeOffset>();
        properties.Should().ContainKey("Operation.DurationMs").WhoseValue.Should().BeOfType<int>();
        properties.Should().ContainKey("Operation.Journal").WhoseValue.Should().BeOfType<string>();
        properties.Should().NotContainKey("Operation.Result");
    }

    [Fact]
    public void Journal_StartsWithOperationStartedLine()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("Name"))
        {
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

        journal.Should().MatchRegex(@"^\[\d+\.\d{3}\] Operation started at ");
    }

    [Fact]
    public void Journal_EndsWithOperationCompletedLine()
    {
        var logger = new RecordingLogger();

        using (logger.BeginOperation("Name"))
        {
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

        journal.Should().MatchRegex(@"Operation completed in \d+\.\d{3} seconds\.$");
    }

    [Fact]
    public void Journal_TimestampsUseInvariantCultureRegardlessOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            var logger = new RecordingLogger();

            using (logger.BeginOperation("Name"))
            {
            }

            var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

            journal.Should().MatchRegex(@"^\[\d+\.\d{3}\] Operation started at ");
            journal.Should().MatchRegex(@"Operation completed in \d+\.\d{3} seconds\.$");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void AddProperty_AddsUnprefixedStructuredProperty()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
            log.AddProperty("UserId", 123);

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void SetResult_SetsOperationResultProperty()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
            log.SetResult("done");

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "done"));
    }

    [Fact]
    public void SetException_SetsExceptionOnFinalEntry()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var log = logger.BeginOperation("Name"))
            log.SetException(exception);

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void SetException_DefaultDoesNotAppendJournalLine()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var log = logger.BeginOperation("Name"))
            log.SetException(exception);

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().NotContain("failed:");
    }

    [Fact]
    public void SetException_RecordEverywhereTrue_AlsoAppendsJournalLine()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var log = logger.BeginOperation("Name"))
            log.SetException(exception, recordEverywhere: true);

        logger.LastException.Should().BeSameAs(exception);

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Name` failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void Append_AddsLineToOperationLog()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
            log.Append("custom line");

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("custom line");
    }

    [Fact]
    public void AppendValue_UsesCallerArgumentExpressionForDefaultName()
    {
        var logger = new RecordingLogger();
        var total = 42.5m;

        using (var log = logger.BeginOperation("Name"))
            log.AppendValue(total);

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`total`: 42.5");
    }

    [Fact]
    public void AppendValue_ExplicitName_OverridesDefault()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
            log.AppendValue(42, "Count");

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Count`: 42");
    }

    [Fact]
    public void AppendJson_RendersIndentedJsonUnderGivenName()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
            log.AppendJson(new { A = 1, B = "x" }, "Payload");

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Payload`:");
        journal.Should().Contain("\"A\": 1");
        journal.Should().Contain("\"B\": \"x\"");
    }

    [Fact]
    public void BeginSubOperation_AppendsStartedAndCompleteLines()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
        {
            using var subLog = log.BeginSubOperation("Fetch");
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` started.");
        journal.Should().Contain("`Fetch` complete.");
    }

    [Fact]
    public void SubOperation_AddProperty_AddsToRootFinalEntry()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
        {
            using var subLog = log.BeginSubOperation("Fetch");
            subLog.AddProperty("Count", 5);
        }

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void SubOperation_SetResult_AppendsJournalLine_NotRootResultProperty()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
        {
            using var subLog = log.BeginSubOperation("Fetch");
            subLog.SetResult(99);
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` result: 99");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void SubOperation_SetException_DefaultDoesNotRecordEverywhere()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var log = logger.BeginOperation("Name"))
        {
            using var subLog = log.BeginSubOperation("Fetch");
            subLog.SetException(exception);
        }

        logger.LastException.Should().BeNull();

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void SubOperation_SetException_RecordEverywhereTrue_SetsRootException()
    {
        var logger = new RecordingLogger();
        var exception = new InvalidOperationException("boom");

        using (var log = logger.BeginOperation("Name"))
        {
            using var subLog = log.BeginSubOperation("Fetch");
            subLog.SetException(exception, recordEverywhere: true);
        }

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void NestedSubOperations_AllContributeToSameJournal()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
        {
            using var outerLog = log.BeginSubOperation("Outer");
            using var innerLog = outerLog.BeginSubOperation("Inner");
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Outer` started.");
        journal.Should().Contain("`Inner` started.");
        journal.Should().Contain("`Inner` complete.");
        journal.Should().Contain("`Outer` complete.");
    }

    [Fact]
    public void RootDispose_IsIdempotent()
    {
        var logger = new RecordingLogger();

        var log = logger.BeginOperation("Name");
        log.Dispose();
        log.Dispose();

        logger.LogCallCount.Should().Be(1);
    }

    [Fact]
    public void ChildDispose_IsIdempotent()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name"))
        {
            var subLog = log.BeginSubOperation("Fetch");
            subLog.Dispose();
            subLog.Dispose();
        }

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        CountOccurrences(journal, "`Fetch` complete.").Should().Be(1);
    }

    [Fact]
    public async Task ThreadSafe_AllowsConcurrentSubOperations()
    {
        var logger = new RecordingLogger();

        using (var log = logger.BeginOperation("Name", threadSafe: true))
        {
            var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                using var subLog = log.BeginSubOperation($"Sub{i}");
                subLog.AddProperty($"P{i}", i);
                subLog.AppendValue(i, "value");
            }));

            await Task.WhenAll(tasks);
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastProperties!.Count(kvp => kvp.Key.StartsWith('P')).Should().Be(20);

        var journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        for (var i = 0; i < 20; i++)
        {
            journal.Should().Contain($"`Sub{i}` started.");
            journal.Should().Contain($"`Sub{i}` complete.");
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
