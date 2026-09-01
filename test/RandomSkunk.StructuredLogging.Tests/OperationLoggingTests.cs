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

            using ISubOperationLog subLog = log.BeginSubOperation("Sub");
            subLog.AddProperty("y", 2).AppendResult(3).AppendException(new InvalidOperationException());
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
        logger.LastMessage.Should().StartWith("Operation: DoThing\n");
        logger.LastMessage.Should().EndWith("Operation complete.");
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
        properties.Should().ContainKey("Operation.StartTime").WhoseValue.Should().BeOfType<DateTime>()
            .Which.Kind.Should().Be(DateTimeKind.Utc);
        properties.Should().ContainKey("Operation.DurationSeconds").WhoseValue.Should().BeOfType<double>();
        properties.Should().NotContainKey("Operation.Result");
        logger.LastMessage.Should().NotBeNull();
    }

    [Fact]
    public void Journal_HeaderIncludesStartTimeLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        string journal = logger.LastMessage!;
        // Operation.StartTime is UTC (for cross-timezone correlation); the journal header prints
        // local time instead, for a human reading the entry in their own context - convert back
        // to compare them, since the local value itself isn't exposed anywhere.
        DateTime startTimeUtc = (DateTime)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.StartTime").Value!;
        DateTimeOffset startTime = startTimeUtc.ToLocalTime();

        string startTimeLine = "Start Time: " + startTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        const string propertiesLines = "Properties:\n- Operation.Name\n- Operation.StartTime\n- Operation.DurationSeconds";
        string dashes = new string('-', 40);

        journal.Should().StartWith($"Operation: Name\n{startTimeLine}\n{propertiesLines}\n{dashes}\n");
        journal.Should().NotContain("Operation started at");
    }

    [Fact]
    public void Journal_HeaderPropertiesLineIncludesResultAndAddedProperties()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            log.AddProperty("Foo", 1).AddProperty("Bar", 2).SetResult("done");
        }

        string journal = logger.LastMessage!;

        journal.Should().Contain("\nProperties:\n- Operation.Name\n- Operation.StartTime\n- Operation.DurationSeconds\n- Operation.Result\n- Foo\n- Bar\n");
    }

    [Fact]
    public void Journal_HeaderPropertiesLineOmitsResultWhenNotSet()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        string journal = logger.LastMessage!;

        journal.Should().Contain("\nProperties:\n- Operation.Name\n- Operation.StartTime\n- Operation.DurationSeconds\n");
        journal.Should().NotContain("Operation.Result");
    }

    [Fact]
    public void Journal_EndsWithOperationCompletedLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation("Name"))
        {
        }

        string journal = logger.LastMessage!;

        journal.Should().MatchRegex(@"\[\d+\.\d{3}\] Operation complete\.$");
    }

    [Fact]
    public void Journal_WithNonDefaultEventId_IncludesEventIdHeaderLine()
    {
        RecordingLogger logger = new();

        using (logger.BeginOperation(new EventId(42, "SomeEvent"), "Name"))
        {
        }

        string journal = logger.LastMessage!;
        // Operation.StartTime is UTC (for cross-timezone correlation); the journal header prints
        // local time instead, for a human reading the entry in their own context - convert back
        // to compare them, since the local value itself isn't exposed anywhere.
        DateTime startTimeUtc = (DateTime)logger.LastProperties!.Single(kvp => kvp.Key == "Operation.StartTime").Value!;
        DateTimeOffset startTime = startTimeUtc.ToLocalTime();

        string startTimeLine = "Start Time: " + startTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        const string propertiesLines = "Properties:\n- Operation.Name\n- Operation.StartTime\n- Operation.DurationSeconds";
        string dashes = new string('-', 40);

        journal.Should().StartWith($"Operation: Name\nEvent Id: SomeEvent\n{startTimeLine}\n{propertiesLines}\n{dashes}\n");
    }

    [Fact]
    public void Journal_WithUnnamedEventId_UsesEventIdNumberInHeaderLine()
    {
        RecordingLogger logger = new();
        EventId eventId = new(42);

        using (logger.BeginOperation(eventId, "Name"))
        {
        }

        string journal = logger.LastMessage!;

        // An EventId with no name renders as its number - the header has to match EventId.ToString()
        // exactly, since that's what it used to be built from.
        journal.Should().Contain("\nEvent Id: 42\n");
        journal.Should().Contain($"\nEvent Id: {eventId}\n");
    }

    [Fact]
    public void Journal_WithNegativeUnnamedEventId_UsesInvariantNegativeSign()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            RecordingLogger logger = new();

            using (logger.BeginOperation(new EventId(-7), "Name"))
            {
            }

            logger.LastMessage.Should().Contain("\nEvent Id: -7\n");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
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

            string journal = logger.LastMessage!;

            journal.Should().MatchRegex(@"^Operation: Name\nStart Time: \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}\nProperties:\n- Operation\.Name\n- Operation\.StartTime\n- Operation\.DurationSeconds\n-+\n");
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
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
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
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");

        subLog.EventId.Should().Be(eventId);
    }

    [Fact]
    public void SubOperation_Disabled_SharesEventIdAndPropertiesWithRoot()
    {
        RecordingLogger logger = new() { Enabled = false };
        EventId eventId = new(42, "Custom");

        using IOperationLog log = logger.BeginOperation(eventId, "Name");
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
        subLog.AddProperty("Count", 5);

        subLog.EventId.Should().Be(eventId);
        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void IsEnabled_True_WhenLevelIsEnabled()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");

        log.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_False_WhenLevelIsDisabled()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");

        log.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_ThreadSafe_MatchesInnerLog()
    {
        RecordingLogger enabledLogger = new();
        RecordingLogger disabledLogger = new() { Enabled = false };

        using IOperationLog enabledLog = enabledLogger.BeginOperation("Name", threadSafe: true);
        using IOperationLog disabledLog = disabledLogger.BeginOperation("Name", threadSafe: true);

        enabledLog.IsEnabled.Should().BeTrue();
        disabledLog.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SubOperation_IsEnabled_MatchesRoots()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");

        subLog.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SubOperation_Disabled_IsEnabledMatchesRoots()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");

        subLog.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Escalate_DoesNotChangeIsEnabled()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        log.Escalate(LogLevel.Critical);

        log.IsEnabled.Should().BeFalse();
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
    public void SetResult_AppendsMarkerJournalLine_DoesNotAppendFormattedValue()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetResult("done");

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation result set.");
        journal.Should().NotContain("Operation result: done");
    }

    [Fact]
    public void SetResult_CalledTwice_AppendsOverwriteMarkerOnSecondCall()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetResult("first").SetResult("second");

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation result set.");
        journal.Should().Contain("Operation result set again, overwriting the previous value.");
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
    public void SetException_AppendsMarkerJournalLine_DoesNotAppendExceptionText()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(exception);

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation exception set.");
        journal.Should().NotContain("failed:");
        journal.Should().NotContain(exception.ToString());
    }

    [Fact]
    public void SetException_CalledTwice_AppendsOverwriteMarkerOnSecondCall()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(new InvalidOperationException("first")).SetException(new InvalidOperationException("second"));

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation exception set.");
        journal.Should().Contain("Operation exception set again, overwriting the previous value.");
    }

    [Fact]
    public void AppendResult_AppendsJournalLine_DoesNotSetOperationResultProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendResult("done");

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation result: done");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void AppendException_AppendsJournalLine_DoesNotSetExceptionOnFinalEntry()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendException(exception);

        logger.LastException.Should().BeNull();

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void Escalate_MoreSevere_RaisesLevelOnFinalEntry()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Escalate(LogLevel.Error);

        logger.LastLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void Escalate_LessSevere_DoesNotLowerLevel()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", LogLevel.Warning))
            log.Escalate(LogLevel.Information);

        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Escalate_ReturnsSameLogForChaining()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.SetException(exception).Escalate(LogLevel.Error);

        logger.LastException.Should().BeSameAs(exception);
        logger.LastLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void SubOperation_Escalate_RaisesLevelOnRootFinalEntry()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.Escalate(LogLevel.Critical);
        }

        logger.LastLevel.Should().Be(LogLevel.Critical);
    }

    [Fact]
    public void Disabled_Escalate_DoesNotEnableLogging()
    {
        RecordingLogger logger = new() { Enabled = false };

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Escalate(LogLevel.Critical);

        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Escalate_MoreSevere_AppendsJournalLineWithPreviousAndNewLevel()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Escalate(LogLevel.Error);

        string journal = logger.LastMessage!;
        journal.Should().Contain("Operation escalated from Information to Error.");
    }

    [Fact]
    public void Escalate_LessSevere_DoesNotAppendJournalLine()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", LogLevel.Warning))
            log.Escalate(LogLevel.Information);

        string journal = logger.LastMessage!;
        journal.Should().NotContain("escalated");
    }

    [Fact]
    public void Escalate_SameLevel_DoesNotAppendJournalLine()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", LogLevel.Warning))
            log.Escalate(LogLevel.Warning);

        string journal = logger.LastMessage!;
        journal.Should().NotContain("escalated");
    }

    [Fact]
    public void SubOperation_Escalate_AppendsJournalLineWithSubOperationName()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.Escalate(LogLevel.Critical);
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch` escalated from Information to Critical.");
    }

    [Fact]
    public void Disabled_Escalate_DoesNotThrow()
    {
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        log.Escalate(LogLevel.Critical);
    }

    [Fact]
    public void Append_AddsLineToOperationLog()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Append("custom line");

        string journal = logger.LastMessage!;
        journal.Should().Contain("custom line");
    }

    [Fact]
    public void Append_Interpolated_AddsLineToOperationLog()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Append($"reserved {5} of {10}");

        string journal = logger.LastMessage!;
        journal.Should().Contain("reserved 5 of 10");
    }

    [Fact]
    public void Append_Interpolated_Disabled_DoesNotEvaluateInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.Append($"Value: {GetValue()}");

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Append_Interpolated_ThreadSafe_AddsLineToOperationLog()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
            log.Append($"reserved {5} of {10}");

        string journal = logger.LastMessage!;
        journal.Should().Contain("reserved 5 of 10");
    }

    [Fact]
    public void Append_Interpolated_ThreadSafe_Disabled_DoesNotEvaluateInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
            log.Append($"Value: {GetValue()}");

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void AppendValue_UsesCallerArgumentExpressionForDefaultName()
    {
        RecordingLogger logger = new();
        decimal total = 42.5m;

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(total);

        string journal = logger.LastMessage!;
        journal.Should().Contain("`total`: 42.5");
    }

    [Fact]
    public void AppendValue_ExplicitName_OverridesDefault()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(42, "Count");

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Count`: 42");
    }

    [Fact]
    public void AppendJson_RendersIndentedJsonUnderGivenName()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendJson(new { A = 1, B = "x" }, "Payload");

        string journal = logger.LastMessage!;
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
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch` started.");
        journal.Should().Contain("`Fetch` complete.");
    }

    [Fact]
    public void BeginSubOperation_Interpolated_AppendsStartedAndCompleteLinesWithInterpolatedName()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation($"Fetch-{42}");
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch-42` started.");
        journal.Should().Contain("`Fetch-42` complete.");
    }

    [Fact]
    public void BeginSubOperation_Interpolated_NameIsReusedInAppendResultAndAppendException()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation($"Fetch-{42}");
            subLog.AppendResult("done").AppendException(new InvalidOperationException("boom"));
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch-42` result: done");
        journal.Should().Contain("`Fetch-42` failed:");
    }

    [Fact]
    public void BeginSubOperation_Interpolated_Disabled_DoesNotEvaluateInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        using (IOperationLog log = logger.BeginOperation("Name"))
        using (ISubOperationLog subLog = log.BeginSubOperation($"Fetch-{GetValue()}"))
        {
        }

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void BeginSubOperation_Interpolated_ThreadSafe_AppendsStartedAndCompleteLines()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
        {
            using ISubOperationLog subLog = log.BeginSubOperation($"Fetch-{42}");
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch-42` started.");
        journal.Should().Contain("`Fetch-42` complete.");
    }

    [Fact]
    public void BeginSubOperation_Interpolated_ThreadSafe_Disabled_DoesNotEvaluateInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
        using (ISubOperationLog subLog = log.BeginSubOperation($"Fetch-{GetValue()}"))
        {
        }

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void SubOperation_AddProperty_AddsToRootFinalEntry()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AddProperty("Count", 5);
        }

        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Count", 5));
    }

    [Fact]
    public void SubOperation_AppendResult_AppendsJournalLine_NotRootResultProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AppendResult(99);
        }

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch` result: 99");
        logger.LastProperties!.Any(kvp => kvp.Key == "Operation.Result").Should().BeFalse();
    }

    [Fact]
    public void SubOperation_AppendException_AppendsJournalLineAndDoesNotSetRootException()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AppendException(exception);
        }

        logger.LastException.Should().BeNull();

        string journal = logger.LastMessage!;
        journal.Should().Contain("`Fetch` failed:");
        journal.Should().Contain(exception.ToString());
    }

    [Fact]
    public void NestedSubOperations_AllContributeToSameJournal()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog outerLog = log.BeginSubOperation("Outer");
            using ISubOperationLog innerLog = outerLog.BeginSubOperation("Inner");
        }

        string journal = logger.LastMessage!;
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
            ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.Dispose();
            subLog.Dispose();
        }

        string journal = logger.LastMessage!;
        CountOccurrences(journal, "`Fetch` complete.").Should().Be(1);
    }

    [Fact]
    public void LeakedSubOperation_UsedAfterRootDisposed_ThrowsObjectDisposedException()
    {
        RecordingLogger logger = new();

        ISubOperationLog leakedSubLog;
        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            leakedSubLog = log.BeginSubOperation("Fetch");
        }

        Action append = () => leakedSubLog.Append("late write");

        append.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void LeakedSubOperation_DisposedAfterRootDisposed_ThrowsObjectDisposedException()
    {
        RecordingLogger logger = new();

        ISubOperationLog leakedSubLog;
        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            leakedSubLog = log.BeginSubOperation("Fetch");
        }

        Action dispose = leakedSubLog.Dispose;

        dispose.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void LeakedSubOperation_EscalatedAfterRootDisposed_ThrowsObjectDisposedException()
    {
        RecordingLogger logger = new();

        ISubOperationLog leakedSubLog;
        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            leakedSubLog = log.BeginSubOperation("Fetch");
        }

        Action escalate = () => leakedSubLog.Escalate(LogLevel.Information);

        escalate.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ThreadSafe_AllowsConcurrentSubOperations()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name", threadSafe: true))
        {
            IEnumerable<Task> tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                using ISubOperationLog subLog = log.BeginSubOperation($"Sub{i}");
                subLog.AddProperty($"P{i}", i);
                subLog.AppendValue(i, "value");
            }));

            await Task.WhenAll(tasks);
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastProperties!.Count(kvp => kvp.Key.StartsWith('P')).Should().Be(20);

        string journal = logger.LastMessage!;
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

    [Fact]
    public void AppendValue_NullValue_RendersNullLiteral()
    {
        RecordingLogger logger = new();
        string? missing = null;

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(missing);

        logger.LastMessage.Should().Contain("`missing`: null");
    }

    [Fact]
    public void AppendValue_NonFormattableValue_UsesToString()
    {
        RecordingLogger logger = new();
        NonFormattable value = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(value);

        logger.LastMessage.Should().Contain("`value`: custom-to-string");
    }

    [Fact]
    public void AppendValue_ToStringReturnsNull_RendersNullLiteral()
    {
        RecordingLogger logger = new();
        NullToString value = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AppendValue(value);

        logger.LastMessage.Should().Contain("`value`: null");
    }

    [Fact]
    public void AppendValue_UsesInvariantCultureRegardlessOfCurrentCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            RecordingLogger logger = new();
            decimal total = 42.5m;
            double ratio = 0.25d;

            using (IOperationLog log = logger.BeginOperation("Name"))
                log.AppendValue(total).AppendValue(ratio);

            // fr-FR would render these as "42,5" / "0,25" if the current culture leaked in.
            logger.LastMessage.Should().Contain("`total`: 42.5");
            logger.LastMessage.Should().Contain("`ratio`: 0.25");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void SubOperation_AppendResult_NullValue_RendersNullLiteral()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AppendResult<string?>(null);
        }

        logger.LastMessage.Should().Contain("`Fetch` result: null");
    }

    [Fact]
    public void AddProperty_NullName_ThrowsArgumentNullException()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");
        Func<IOperationLog> act = () => log.AddProperty(null!, 1);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("propertyName");
    }

    [Fact]
    public void AddProperty_NullName_SubOperation_ThrowsArgumentNullException()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name");
        using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
        Func<ISubOperationLog> act = () => subLog.AddProperty(null!, 1);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("propertyName");
    }

    [Fact]
    public void AddProperty_NullName_ThreadSafe_ThrowsArgumentNullException()
    {
        RecordingLogger logger = new();

        using IOperationLog log = logger.BeginOperation("Name", threadSafe: true);
        Func<IOperationLog> act = () => log.AddProperty(null!, 1);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("propertyName");
    }

    [Fact]
    public void AddProperty_NullName_Disabled_ThrowsArgumentNullException()
    {
        // A disabled operation journals nothing, but it still has to reject a null name the same way an
        // enabled one does - otherwise the bug lies dormant until someone turns the level on.
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        Func<IOperationLog> act = () => log.AddProperty(null!, 1);

        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("propertyName");
    }

    [Fact]
    public void AddProperty_NullName_LeavesTheOperationUsable()
    {
        // The null name used to survive AddProperty and only throw later, out of FinalizeHeader during
        // Dispose - which killed the entire log entry and masked any exception already unwinding through
        // the `using`. Rejecting it at the call site has to leave the operation itself intact.
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            Func<IOperationLog> act = () => log.AddProperty(null!, 1);
            act.Should().Throw<ArgumentNullException>();

            log.AddProperty("UserId", 123).Append("still working");
        }

        logger.LogCallCount.Should().Be(1);
        logger.LastMessage.Should().Contain("still working");
        logger.LastMessage.Should().EndWith("Operation complete.");
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("UserId", 123));
    }

    [Fact]
    public void AddProperty_ReservedName_WarnsInJournalButStillAddsTheProperty()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AddProperty("Operation.Result", 123);

        logger.LastMessage.Should().Contain(
            "\"Operation.Result\" is a reserved property name; the operation's own property of that name will be duplicated in the log entry.");
        logger.LastProperties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Result", 123));
    }

    [Fact]
    public void AddProperty_ReservedName_SubOperation_WarnsInJournal()
    {
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
        {
            using ISubOperationLog subLog = log.BeginSubOperation("Fetch");
            subLog.AddProperty("Operation.Name", "oops");
        }

        logger.LastMessage.Should().Contain(
            "\"Operation.Name\" is a reserved property name; the operation's own property of that name will be duplicated in the log entry.");
    }

    [Fact]
    public void AddProperty_ReservedName_Disabled_DoesNotThrow()
    {
        // A disabled operation journals nothing, so there's nowhere to warn into - but adding a
        // reserved-prefixed property must still succeed rather than throw, same as any other name.
        RecordingLogger logger = new() { Enabled = false };

        using IOperationLog log = logger.BeginOperation("Name");
        Action act = () => log.AddProperty("Operation.Name", "oops");

        act.Should().NotThrow();
        log.Properties.Should().ContainEquivalentOf(new KeyValuePair<string, object?>("Operation.Name", "oops"));
    }

    [Fact]
    public void AddProperty_UnreservedNameStartingWithOperation_DoesNotWarn()
    {
        // A name that merely starts with the word "Operation" but isn't in the reserved namespace is
        // an ordinary property.
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AddProperty("OperationCount", 5);

        logger.LastMessage.Should().NotContain("reserved property name");
    }

    [Fact]
    public void AddProperty_UnreservedNameUnderTheOperationPrefix_DoesNotWarn()
    {
        // Only the four names the library actually writes are reserved - a name that merely shares the
        // "Operation." prefix but isn't one of them doesn't collide with anything, so it isn't flagged.
        RecordingLogger logger = new();

        using (IOperationLog log = logger.BeginOperation("Name"))
            log.AddProperty("Operation.Foo", 5);

        logger.LastMessage.Should().NotContain("reserved property name");
    }

    [Fact]
    public void Dispose_WhenTheSinkThrows_StillDisposesTheSharedState()
    {
        // Whatever goes wrong while flushing the entry, the shared state has to end up disposed: that's
        // what returns the pooled journal and what makes a sub-operation that outlived the root throw
        // instead of writing into a StringBuilder the pool has already handed to someone else.
        RecordingLogger logger = new() { ThrowOnLog = new InvalidOperationException("sink failed") };

        IOperationLog log = logger.BeginOperation("Name");
        ISubOperationLog leakedSubLog = log.BeginSubOperation("Fetch");

        Action dispose = log.Dispose;
        dispose.Should().Throw<InvalidOperationException>().WithMessage("sink failed");

        logger.LogCallCount.Should().Be(1);

        Action append = () => leakedSubLog.Append("late write");
        append.Should().Throw<ObjectDisposedException>();
    }

    private sealed class NonFormattable
    {
        public override string ToString() => "custom-to-string";
    }

    private sealed class NullToString
    {
        public override string? ToString() => null;
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
