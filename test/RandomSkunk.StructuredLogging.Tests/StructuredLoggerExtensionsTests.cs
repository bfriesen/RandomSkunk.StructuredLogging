using AwesomeAssertions;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class StructuredLoggerExtensionsTests
{
    [Fact]
    public void MessageOnly_LogsFormattedMessage()
    {
        RecordingLogger logger = new();
        string name = "World";

        logger.Information($"Hello, {name}!");

        logger.LogCallCount.Should().Be(1);
        logger.LastLevel.Should().Be(LogLevel.Information);
        logger.LastMessage.Should().Be("Hello, World!");
        logger.LastProperties.Should().BeEmpty();
        logger.LastEventId.Should().Be(default(EventId));
        logger.LastException.Should().BeNull();
    }

    [Fact]
    public void StringLiteral_UsesPlainMessageOverloadWithoutFormatting()
    {
        RecordingLogger logger = new();

        logger.Warning("plain message");

        logger.LastMessage.Should().Be("plain message");
        logger.LastLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Disabled_DoesNotEvaluateInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        logger.Debug($"Value: {GetValue()}");

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Disabled_StringLiteralStillSkipsLogging()
    {
        RecordingLogger logger = new() { Enabled = false };

        logger.Debug("plain message");

        logger.LogCallCount.Should().Be(0);
    }

    [Fact]
    public void Enabled_EvaluatesInterpolationHoles()
    {
        RecordingLogger logger = new() { Enabled = true };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 42;
        }

        logger.Debug($"Value: {GetValue()}");

        evaluationCount.Should().Be(1);
        logger.LastMessage.Should().Be("Value: 42");
    }

    [Fact]
    public void SingleProperty_IsPassedThrough()
    {
        RecordingLogger logger = new();

        logger.Debug($"msg", ("UserId", 42));

        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("UserId", 42));
    }

    [Fact]
    public void SixProperties_MaxArityIsPassedThrough()
    {
        RecordingLogger logger = new();

        logger.Debug(
            $"msg",
            ("P1", 1),
            ("P2", "two"),
            ("P3", 3.0),
            ("P4", true),
            ("P5", 'c'),
            ("P6", 6L));

        logger.LastProperties.Should().HaveCount(6);
        logger.LastProperties!.Select(p => p.Key).Should().Equal("P1", "P2", "P3", "P4", "P5", "P6");
        logger.LastProperties!.Select(p => p.Value).Should().Equal(1, "two", 3.0, true, 'c', 6L);
    }

    [Fact]
    public void CollectionOverload_AcceptsListWithNoAdditionalProperties()
    {
        RecordingLogger logger = new();
        List<KeyValuePair<string, object?>> properties = new()
        {
            new("A", 1),
            new("B", "two"),
        };

        logger.Debug(properties, $"msg");

        logger.LastProperties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void CollectionOverload_AcceptsDictionaryWithNoAdditionalProperties()
    {
        RecordingLogger logger = new();
        Dictionary<string, object?> properties = new()
        {
            ["A"] = 1,
            ["B"] = "two",
        };

        logger.Debug(properties, $"msg");

        logger.LastProperties.Should().BeEquivalentTo(properties);
    }

    [Fact]
    public void CollectionOverload_CombinesWithSingleStaticProperty()
    {
        RecordingLogger logger = new();
        Dictionary<string, object?> properties = new() { ["A"] = 1 };

        logger.Debug(properties, $"msg", ("B", "two"));

        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("A", 1),
            new KeyValuePair<string, object?>("B", "two"));
    }

    [Fact]
    public void CollectionOverload_CombinesWithParamsProperties()
    {
        RecordingLogger logger = new();
        Dictionary<string, object?> properties = new() { ["A"] = 1 };

        logger.Debug(properties, $"msg", ("B", "two"), ("C", 3.0));

        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("A", 1),
            new KeyValuePair<string, object?>("B", "two"),
            new KeyValuePair<string, object?>("C", 3.0));
    }

    [Fact]
    public void EventIdAndException_BothPassedThrough()
    {
        RecordingLogger logger = new();
        EventId eventId = new(7, "SomethingHappened");
        InvalidOperationException exception = new("boom");

        logger.Error(eventId, exception, $"failure");

        logger.LastEventId.Should().Be(eventId);
        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void EventIdOnly_ExceptionDefaultsToNull()
    {
        RecordingLogger logger = new();
        EventId eventId = new(7, "SomethingHappened");

        logger.Error(eventId, $"failure");

        logger.LastEventId.Should().Be(eventId);
        logger.LastException.Should().BeNull();
    }

    [Fact]
    public void ExceptionOnly_EventIdDefaultsToDefault()
    {
        RecordingLogger logger = new();
        InvalidOperationException exception = new("boom");

        logger.Error(exception, $"failure");

        logger.LastEventId.Should().Be(default(EventId));
        logger.LastException.Should().BeSameAs(exception);
    }

    [Fact]
    public void Neither_EventIdAndExceptionDefault()
    {
        RecordingLogger logger = new();

        logger.Error($"failure");

        logger.LastEventId.Should().Be(default(EventId));
        logger.LastException.Should().BeNull();
    }

    [Fact]
    public void Write_UsesProvidedLevel()
    {
        RecordingLogger logger = new();

        logger.Write(LogLevel.Critical, $"custom level");

        logger.LastLevel.Should().Be(LogLevel.Critical);
        logger.LastMessage.Should().Be("custom level");
    }

    [Fact]
    public void Write_RespectsIsEnabledForProvidedLevel()
    {
        RecordingLogger logger = new() { Enabled = false };
        int evaluationCount = 0;

        int GetValue()
        {
            evaluationCount++;
            return 1;
        }

        logger.Write(LogLevel.Trace, $"{GetValue()}");

        evaluationCount.Should().Be(0);
        logger.LogCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void AllLevels_LogAtTheCorrectLevel(LogLevel level)
    {
        RecordingLogger logger = new();

        switch (level)
        {
            case LogLevel.Trace: logger.Trace($"msg"); break;
            case LogLevel.Debug: logger.Debug($"msg"); break;
            case LogLevel.Information: logger.Information($"msg"); break;
            case LogLevel.Warning: logger.Warning($"msg"); break;
            case LogLevel.Error: logger.Error($"msg"); break;
            case LogLevel.Critical: logger.Critical($"msg"); break;
        }

        logger.LastLevel.Should().Be(level);
    }

    [Fact]
    public void FormatSpecifier_IsApplied()
    {
        RecordingLogger logger = new();
        double value = 3.14159;

        logger.Information($"Pi is {value:F2}");

        logger.LastMessage.Should().Be("Pi is 3.14");
    }

    [Fact]
    public void Alignment_PadsFormattedValue()
    {
        RecordingLogger logger = new();
        int value = 7;

        logger.Information($"[{value,5}]");

        logger.LastMessage.Should().Be("[    7]");
    }

    [Fact]
    public void NegativeAlignment_LeftAlignsFormattedValue()
    {
        RecordingLogger logger = new();
        int value = 7;

        logger.Information($"[{value,-5}]");

        logger.LastMessage.Should().Be("[7    ]");
    }

    [Fact]
    public void EnumeratorMatchesIndexer_AcrossPropertyShapes()
    {
        RecordingLogger logger = new();
        Dictionary<string, object?> collection = new() { ["A"] = 1, ["B"] = 2 };
        string name = "Alice";

        // Pins the documented property order - the collection's entries, then tag-captured properties,
        // then the trailing per-call properties - across every shape that combines them, including the
        // 0-arity state, which is a separate hand-written type from the generic ones. Sinks read the
        // state both by index and by foreach, so the two paths also have to agree; GetEnumerator
        // currently delegates to the indexer, and this keeps that honest if it is ever reimplemented.
        logger.Debug($"plain");
        KeysOf(logger).Should().BeEmpty();

        logger.Debug($"Hello, {name:<UserName>}!");
        KeysOf(logger).Should().Equal("UserName");

        logger.Debug(collection, $"Hello, {name:<UserName>}!");
        KeysOf(logger).Should().Equal("A", "B", "UserName");

        logger.Debug(collection, $"Hello, {name:<UserName>}!", ("C", 3));
        KeysOf(logger).Should().Equal("A", "B", "UserName", "C");

        logger.Debug(collection, $"Hello, {name:<UserName>}!", ("C", 3), ("D", 4), ("E", 5), ("F", 6), ("G", 7), ("H", 8));
        KeysOf(logger).Should().Equal("A", "B", "UserName", "C", "D", "E", "F", "G", "H");
    }

    /// <summary>
    /// Asserts that enumerating the last logged state yields exactly what indexing it 0..Count-1 does,
    /// then returns the property names in order so the caller can assert on the sequence itself.
    /// </summary>
    private static List<string> KeysOf(RecordingLogger logger)
    {
        IReadOnlyList<KeyValuePair<string, object?>> properties = logger.LastProperties!;

        List<KeyValuePair<string, object?>> enumerated = new();
        foreach (KeyValuePair<string, object?> property in properties)
            enumerated.Add(property);

        List<KeyValuePair<string, object?>> indexed = new();
        for (int i = 0; i < properties.Count; i++)
            indexed.Add(properties[i]);

        enumerated.Should().Equal(indexed);

        return enumerated.Select(property => property.Key).ToList();
    }
}
