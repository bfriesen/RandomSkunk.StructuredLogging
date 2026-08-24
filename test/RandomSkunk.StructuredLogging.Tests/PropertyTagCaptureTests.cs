using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class PropertyTagCaptureTests
{
    [Fact]
    public void TaggedFormat_CapturesValueAsProperty_AndKeepsItInMessage()
    {
        RecordingLogger logger = new();
        string name = "World";

        logger.Debug($"Hello, {name:<UserName>}!");

        logger.LastMessage.Should().Be("Hello, World!");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("UserName", "World"));
    }

    [Fact]
    public void TaggedFormat_WithFormatSpecifier_FormatsMessage_AndCapturesRawValue()
    {
        RecordingLogger logger = new();
        DateTime timestamp = new(2024, 1, 2, 13, 14, 15, DateTimeKind.Utc);

        logger.Information($"[{timestamp:<Timestamp>HH:mm:ss}] Login operation started.");

        logger.LastMessage.Should().Be("[13:14:15] Login operation started.");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("Timestamp", timestamp));
    }

    [Fact]
    public void EmptyTag_DoesNotCapture_ButStripsTagFromFormat()
    {
        RecordingLogger logger = new();
        double value = 3.14159;

        logger.Information($"Pi is {value:<>F2}");

        logger.LastMessage.Should().Be("Pi is 3.14");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void UntaggedFormat_BehavesAsBefore()
    {
        RecordingLogger logger = new();
        double value = 3.14159;

        logger.Information($"Pi is {value:F2}");

        logger.LastMessage.Should().Be("Pi is 3.14");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void MultipleTags_CapturesAllInMessageOrder()
    {
        RecordingLogger logger = new();
        string first = "Alice";
        int second = 42;

        logger.Debug($"{first:<UserName>} has {second:<Count>} items.");

        logger.LastMessage.Should().Be("Alice has 42 items.");
        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("UserName", "Alice"),
            new KeyValuePair<string, object?>("Count", 42));
    }

    [Fact]
    public void MixedTaggedAndUntaggedInterpolations_OnlyCapturesTagged()
    {
        RecordingLogger logger = new();
        string name = "Alice";
        string untouched = "ignored";

        logger.Debug($"{name:<UserName>} did something with {untouched}.");

        logger.LastMessage.Should().Be("Alice did something with ignored.");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("UserName", "Alice"));
    }

    [Fact]
    public void TaggedFormat_CombinesWithExplicitGenericProperty()
    {
        RecordingLogger logger = new();
        string name = "Alice";

        logger.Debug($"Hello, {name:<UserName>}!", ("RequestId", 7));

        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("UserName", "Alice"),
            new KeyValuePair<string, object?>("RequestId", 7));
    }

    [Fact]
    public void TaggedFormat_CombinesWithCollectionProperties()
    {
        RecordingLogger logger = new();
        string name = "Alice";
        Dictionary<string, object?> extraProperties = new() { ["RequestId"] = 7 };

        logger.Debug(extraProperties, $"Hello, {name:<UserName>}!");

        logger.LastProperties.Should().HaveCount(2);
        logger.LastProperties![0].Should().Be(new KeyValuePair<string, object?>("UserName", "Alice"));
        logger.LastProperties![1].Should().Be(new KeyValuePair<string, object?>("RequestId", 7));
    }

    [Fact]
    public void Disabled_DoesNotCaptureProperties()
    {
        RecordingLogger logger = new() { Enabled = false };

        string? capturedName = null;
        string GetName() => capturedName = "World";

        logger.Debug($"Hello, {GetName():<UserName>}!");

        capturedName.Should().BeNull();
    }

    [Fact]
    public void TaggedDestructuringFormat_CapturesRawValue_AndRendersDestructuredMessage()
    {
        RecordingLogger logger = new();
        OrderItem item = new(123, 456, 1);

        logger.Trace($"Item added to cart: {item:<@Item>}");

        logger.LastMessage.Should().Be("Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }");
        KeyValuePair<string, object?> property = logger.LastProperties.Should().ContainSingle().Which;
        property.Key.Should().Be("Item");
        property.Value.Should().BeSameAs(item);
    }

    [Fact]
    public void EmptyDestructuringTag_DoesNotCapture_ButRendersDestructuredMessage()
    {
        RecordingLogger logger = new();
        OrderItem item = new(123, 456, 1);

        logger.Trace($"Item added to cart: {item:<@>}");

        logger.LastMessage.Should().Be("Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void TaggedDestructuringFormat_IgnoresTrailingFormatText()
    {
        RecordingLogger logger = new();
        OrderItem item = new(123, 456, 1);

        logger.Trace($"Item: {item:<@Item>SomeIgnoredText}");

        logger.LastMessage.Should().Be("Item: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }");
    }

    [Fact]
    public void Disabled_DoesNotCaptureOrRenderDestructuredMessage()
    {
        RecordingLogger logger = new() { Enabled = false };

        OrderItem? captured = null;
        OrderItem GetItem()
        {
            captured = new OrderItem(1, 2, 3);
            return captured;
        }

        logger.Trace($"Item: {GetItem():<@Item>}");

        captured.Should().BeNull();
    }

    [Fact]
    public void EmptyTagEscapeHatch_CannotAlsoCaptureAProperty()
    {
        // A developer who knows "<>" strips itself out to let a real format starting with '<'
        // survive (see EmptyTag_DoesNotCapture_ButStripsTagFromFormat above) might reasonably
        // guess that stacking a second tag onto the escaped remainder - "<><PropertyName>" -
        // both opts back into capturing *and* keeps the leading '<' available for a real format.
        // It doesn't: only the first "<...>" is ever parsed as a tag. Once the empty tag strips
        // itself, everything after it - including a second "<Amount>" - is passed straight
        // through as literal format text to double.ToString(). That's not a valid custom numeric
        // format string, so .NET doesn't throw - it echoes the unrecognized characters back
        // verbatim - silently replacing the formatted number with the literal text "<Amount>F2"
        // and never capturing an "Amount" property at all.
        RecordingLogger logger = new();
        double value = 3.14159;

        logger.Information($"Total: {value:<><Amount>F2}");

        logger.LastMessage.Should().Be("Total: 3.14");
        logger.LastProperties.Should().ContainSingle()
            .Which.Should().Be(new KeyValuePair<string, object?>("Amount", value));
    }

    [Fact]
    public void UnterminatedTag_ThrowsFormatException()
    {
        RecordingLogger logger = new();
        double value = 3.14159;

        Action act = () => logger.Information($"Total: {value:<Amount}");

        act.Should().Throw<UnterminatedLogPropertyTagException>()
            .WithMessage("*<Amount*");
    }

    [Fact]
    public void UnterminatedTag_Disabled_DoesNotThrow()
    {
        RecordingLogger logger = new() { Enabled = false };
        double value = 3.14159;

        Action act = () => logger.Information($"Total: {value:<Amount}");

        act.Should().NotThrow();
    }
}
