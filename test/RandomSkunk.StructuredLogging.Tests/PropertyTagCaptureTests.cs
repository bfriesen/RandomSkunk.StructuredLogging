using AwesomeAssertions;
using Microsoft.Extensions.Logging;

namespace RandomSkunk.StructuredLogging.Tests;

public class PropertyTagCaptureTests
{
    [Fact]
    public void TaggedFormat_CapturesValueAsProperty_AndKeepsItInMessage()
    {
        var logger = new RecordingLogger();
        var name = "World";

        logger.Debug($"Hello, {name:<UserName>}!");

        logger.LastMessage.Should().Be("Hello, World!");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("UserName", "World"));
    }

    [Fact]
    public void TaggedFormat_WithFormatSpecifier_FormatsMessage_AndCapturesRawValue()
    {
        var logger = new RecordingLogger();
        var timestamp = new DateTime(2024, 1, 2, 13, 14, 15, DateTimeKind.Utc);

        logger.Information($"[{timestamp:<Timestamp>HH:mm:ss}] Login operation started.");

        logger.LastMessage.Should().Be("[13:14:15] Login operation started.");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("Timestamp", timestamp));
    }

    [Fact]
    public void EmptyTag_DoesNotCapture_ButStripsTagFromFormat()
    {
        var logger = new RecordingLogger();
        var value = 3.14159;

        logger.Information($"Pi is {value:<>F2}");

        logger.LastMessage.Should().Be("Pi is 3.14");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void UntaggedFormat_BehavesAsBefore()
    {
        var logger = new RecordingLogger();
        var value = 3.14159;

        logger.Information($"Pi is {value:F2}");

        logger.LastMessage.Should().Be("Pi is 3.14");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void MultipleTags_CapturesAllInMessageOrder()
    {
        var logger = new RecordingLogger();
        var first = "Alice";
        var second = 42;

        logger.Debug($"{first:<UserName>} has {second:<Count>} items.");

        logger.LastMessage.Should().Be("Alice has 42 items.");
        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("UserName", "Alice"),
            new KeyValuePair<string, object?>("Count", 42));
    }

    [Fact]
    public void MixedTaggedAndUntaggedInterpolations_OnlyCapturesTagged()
    {
        var logger = new RecordingLogger();
        var name = "Alice";
        var untouched = "ignored";

        logger.Debug($"{name:<UserName>} did something with {untouched}.");

        logger.LastMessage.Should().Be("Alice did something with ignored.");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("UserName", "Alice"));
    }

    [Fact]
    public void TaggedFormat_CombinesWithExplicitGenericProperty()
    {
        var logger = new RecordingLogger();
        var name = "Alice";

        logger.Debug($"Hello, {name:<UserName>}!", ("RequestId", 7));

        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("UserName", "Alice"),
            new KeyValuePair<string, object?>("RequestId", 7));
    }

    [Fact]
    public void TaggedFormat_CombinesWithParamsProperties()
    {
        var logger = new RecordingLogger();
        var name = "Alice";
        var extraProperties = new (string Name, object? Value)[]
        {
            ("RequestId", 7),
            ("Region", "us-east"),
        };

        logger.Debug($"Hello, {name:<UserName>}!", extraProperties);

        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("UserName", "Alice"),
            new KeyValuePair<string, object?>("RequestId", 7),
            new KeyValuePair<string, object?>("Region", "us-east"));
    }

    [Fact]
    public void TaggedFormat_CombinesWithCollectionProperties()
    {
        var logger = new RecordingLogger();
        var name = "Alice";
        var extraProperties = new Dictionary<string, object?> { ["RequestId"] = 7 };

        logger.Debug($"Hello, {name:<UserName>}!", extraProperties);

        logger.LastProperties.Should().HaveCount(2);
        logger.LastProperties![0].Should().Be(new KeyValuePair<string, object?>("UserName", "Alice"));
        logger.LastProperties![1].Should().Be(new KeyValuePair<string, object?>("RequestId", 7));
    }

    [Fact]
    public void Disabled_DoesNotCaptureProperties()
    {
        var logger = new RecordingLogger { Enabled = false };
        var name = "World";

        logger.Debug($"Hello, {name:<UserName>}!");

        logger.LogCallCount.Should().Be(0);
    }
}
