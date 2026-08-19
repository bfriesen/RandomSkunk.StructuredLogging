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
        RecordingLogger logger = new() { Enabled = false };

        using (IOperationLog log = logger.BeginOperation("Test"))
        {
            log
                .AddProperty("x", 1)
                .Append("hi")
                .AppendValue(5)
                .AppendJson(new { A = 1 })
                .SetResult(1)
                .SetException(new InvalidOperationException());

            using IOperationLog subLog = log.BeginSubOperation("Sub");
            subLog.AddProperty("y", 2).SetResult(3).SetException(new InvalidOperationException());
        }

        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Disabled_ThreadSafe_StillReflectsEventIdAndProperties()
    {
        RecordingLogger logger = new() { Enabled = false };
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name", threadSafe: true);
        log.AddProperty("UserId", 123);

        log.EventId.Should().Be(eventId);
        log.OperationName.Should().Be("Name");
        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Enabled_WritesExactlyOneLogEntryOnDispose()
    {
        RecordingLogger logger = new();

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
        RecordingLogger logger = new();
        EventId eventId = new(42, "Custom");

        using (logger.BeginOperation(eventId, "Name"))
        {
        }

        logger.LastEventId.Should().Be(eventId);
    }

    [Fact]
    public void WithLevel_UsesLevelOnFinalEntry()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name", LogLevel.Warning))
        {
        }

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void FinalEntry_IncludesBuiltInOperationProperties()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        Dictionary<string, object?> properties = logger.LastProperties!.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        properties.Should().ContainKey("Operation.Name").WhoseValue.Should().Be("Name");
        properties.Should().ContainKey("Operation.StartTime").WhoseValue.Should().BeOfType<DateTimeOffset>();
        properties.Should().ContainKey("Operation.DurationSeconds").WhoseValue.Should().BeOfType<double>();
        properties.Should().ContainKey("Operation.Journal").WhoseValue.Should().BeOfType<string>();
        properties.Should().NotContainKey("Operation.Result");
    }

    [Fact]
    public void Journal_StartsWithOperationStartedLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

        journal.Should().MatchRegex(@"^Operation: Name\n-{15}\n\[\d+\.\d{3}\] Operation started at ");
    }

    [Fact]
    public void Journal_EndsWithOperationCompletedLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

        journal.Should().MatchRegex(@"\[\d+\.\d{3}\] Operation complete\.$");
    }

    [Fact]
    public void Journal_WithNonDefaultEventId_IncludesEventIdHeaderLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation(new EventId(42, "SomeEvent"), "Name"))
        {
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

        journal.Should().StartWith("Operation: Name\nEventId: SomeEvent\n" + new string('-', "EventId: SomeEvent".Length) + "\n");
    }

    [Fact]
    public void Journal_TimestampsUseInvariantCultureRegardlessOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            RecordingLogger logger = new();

            using (logger.BeginOperation("Name"))
            {
            }

            string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;

            journal.Should().MatchRegex(@"^Operation: Name\n-{15}\n\[\d+\.\d{3}\] Operation started at ");
            journal.Should().MatchRegex(@"\[\d+\.\d{3}\] Operation complete\.$");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void AddProperty_AddsUnprefixedStructuredProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AddProperty("UserId", 123);

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void Properties_ReflectsAddedPropertiesBeforeDispose()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");
        log.AddProperty("UserId", 123);

        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void Properties_Empty_WhenNoPropertiesAdded()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");

        log.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Properties_Disabled_StillReflectsAddedProperties()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        log.AddProperty("UserId", 123);

        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void SubOperation_Properties_SharesRootsPropertyList()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");
        using IOperationLog subLog = log.BeginSubOperation("Fetch");
        subLog.AddProperty("Count", 5);

        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
        subLog.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void ThreadSafe_Properties_ReturnsSnapshotNotLiveList()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name", threadSafe: true);
        log.AddProperty("UserId", 123);

        IReadOnlyList<KeyValuePair<string, object?>> snapshot = log.Properties;
        log.AddProperty("SecondProperty", 456);

        snapshot.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
        snapshot.Should().NotContain(kvp => kvp.Key == "SecondProperty");
    }

    [Fact]
    public void EventId_ReflectsEventIdPassedToBeginOperation()
    {
        RecordingLogger logger = new();
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name");

        log.EventId.Should().Be(eventId);
    }

    [Fact]
    public void EventId_Default_WhenNotProvided()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");

        log.EventId.Should().Be(default(EventId));
    }

    [Fact]
    public void EventId_Disabled_StillReflectsEventId()
    {
        RecordingLogger logger = new() { Enabled = false };
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name");

        log.EventId.Should().Be(eventId);
    }

    [Fact]
    public void SubOperation_EventId_MatchesRoots()
    {
        RecordingLogger logger = new();
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name");
        using IOperationLog subLog = log.BeginSubOperation("Fetch");

        subLog.EventId.Should().Be(eventId);
    }

    [Fact]
    public void SubOperation_Disabled_SharesEventIdAndPropertiesWithRoot()
    {
        RecordingLogger logger = new() { Enabled = false };
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name");
        using IOperationLog subLog = log.BeginSubOperation("Fetch");
        subLog.AddProperty("Count", 5);

        subLog.EventId.Should().Be(eventId);
        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void OperationName_ReflectsNamePassedToBeginOperation()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("FulfillOrder");

        log.OperationName.Should().Be("FulfillOrder");
    }

    [Fact]
    public void OperationName_Disabled_StillReflectsName()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("FulfillOrder");

        log.OperationName.Should().Be("FulfillOrder");
    }

    [Fact]
    public void SubOperation_OperationName_IsItsOwnNameNotRoots()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("FulfillOrder");
        using IOperationLog subLog = log.BeginSubOperation("ChargePayment");

        log.OperationName.Should().Be("FulfillOrder");
        subLog.OperationName.Should().Be("ChargePayment");
    }

    [Fact]
    public void SubOperation_Disabled_OperationNameIsItsOwnNameNotRoots()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("FulfillOrder");
        using IOperationLog subLog = log.BeginSubOperation("ChargePayment");

        log.OperationName.Should().Be("FulfillOrder");
        subLog.OperationName.Should().Be("ChargePayment");
    }

    [Fact]
    public void SetResult_SetsOperationResultProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetResult("done");

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", "done"));
    }

    [Fact]
    public void SetException_SetsExceptionOnFinalEntry()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(exception);

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void SetException_DefaultDoesNotAppendJournalLine()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(exception);

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().NotContain("failed:");
    }

    [Fact]
    public void SetException_RecordEverywhereTrue_AlsoAppendsJournalLine()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(exception, recordEverywhere: true);

        logger.LastException.Should().BeSameAs(exception);

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Name` failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void Append_AddsLineToOperationLog()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Append("custom line");

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("custom line");
    }

    [Fact]
    public void AppendValue_UsesCallerArgumentExpressionForDefaultName()
    {
        RecordingLogger logger = new();
        decimal total = 42.5m;

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(total);

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`total`: 42.5");
    }

    [Fact]
    public void AppendValue_ExplicitName_OverridesDefault()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(42, "Count");

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Count`: 42");
    }

    [Fact]
    public void AppendJson_RendersIndentedJsonUnderGivenName()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendJson(new { A = 1, B = "x" }, "Payload");

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Payload`:");
        journal.Should().Contain("\"A\": 1");
        journal.Should().Contain("\"B\": \"x\"");
    }

    [Fact]
    public void BeginSubOperation_AppendsStartedAndCompleteLines()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog subLog = log.BeginSubOperation("Fetch");
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` started.");
        journal.Should().Contain("`Fetch` complete.");
    }

    [Fact]
    public void SubOperation_AddProperty_AddsToRootFinalEntry()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AddProperty("Count", 5);
        }

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void SubOperation_SetResult_AppendsJournalLine_NotRootResultProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.SetResult(99);
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` result: 99");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void SubOperation_SetException_DefaultDoesNotRecordEverywhere()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.SetException(exception);
        }

        logger.LastException.Should().BeNull();

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Fetch` failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void SubOperation_SetException_RecordEverywhereTrue_SetsRootException()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.SetException(exception, recordEverywhere: true);
        }

        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void NestedSubOperations_AllContributeToSameJournal()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using IOperationLog outerLog = log.BeginSubOperation("Outer");
            using IOperationLog innerLog = outerLog.BeginSubOperation("Inner");
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        journal.Should().Contain("`Outer` started.");
        journal.Should().Contain("`Inner` started.");
        journal.Should().Contain("`Inner` complete.");
        journal.Should().Contain("`Outer` complete.");
    }

    [Fact]
    public void RootDispose_IsIdempotent()
    {
        RecordingLogger logger = new();

        IOperationLog log = logger.BeginOperation("Name");
        log.Dispose();
        log.Dispose();

        logger.LogCallCount.Should().Be(1);
    }

    [Fact]
    public void ChildDispose_IsIdempotent()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            IOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.Dispose();
            subLog.Dispose();
        }

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        CountOccurrences(journal, "`Fetch` complete.").Should().Be(1);
    }

    [Fact]
    public async Task ThreadSafe_AllowsConcurrentSubOperations()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
        {
            IEnumerable<Task> tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                using IOperationLog subLog = log.BeginSubOperation($"Sub{i}");
                subLog.AddProperty($"P{i}", i);
                subLog.AppendValue(i, "value");
            }));

            await Task.WhenAll(tasks);
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastProperties!.Count(kvp => kvp.Key.StartsWith('P')).Should().Be(20);

        string journal = (string)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.Journal").Value!;
        for (int i = 0; i < 20; i++)
        {
            journal.Should().Contain($"`Sub{i}` started.");
            journal.Should().Contain($"`Sub{i}` complete.");
        }
    }

    [Fact]
    public void BeginOperation_NullLogger_ThrowsArgumentNullException()
    {
        ILogger logger = null!;

        Func<IOperationLog> act1 = () => logger.BeginOperation("Name");
        Func<IOperationLog> act2 = () => logger.BeginOperation(default, "Name");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
