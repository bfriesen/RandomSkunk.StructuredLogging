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

        // The collection's entries come first, then tag-captured properties - the order the README
        // documents and every other arity already used.
        logger.LastProperties.Should().Equal(
            new KeyValuePair<string, object?>("RequestId", 7),
            new KeyValuePair<string, object?>("UserName", "Alice"));
    }

    [Fact]
    public void CollectionAndTagOrder_DoesNotDependOnPerCallPropertyCount()
    {
        RecordingLogger logger = new();
        string name = "Alice";
        Dictionary<string, object?> extraProperties = new() { ["RequestId"] = 7 };

        logger.Debug(extraProperties, $"Hello, {name:<UserName>}!");
        List<string> withoutPerCallProperty = logger.LastProperties!.Select(p => p.Key).ToList();

        logger.Debug(extraProperties, $"Hello, {name:<UserName>}!", ("Extra", 1));
        List<string> withPerCallProperty = logger.LastProperties!.Select(p => p.Key).ToList();

        // Adding a per-call property must append to the end, never reshuffle what was already there:
        // the 0-arity overload used to put captured properties before the collection while every
        // other arity did the opposite, so this call pair silently flipped duplicate-key precedence.
        withoutPerCallProperty.Should().Equal("RequestId", "UserName");
        withPerCallProperty.Should().Equal("RequestId", "UserName", "Extra");
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
        property.Key.Should().Be("@Item");
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
    public void TaggedDestructuringFormat_WithTrailingFormat_HonorsFormatForMessage_AndPrefixesCapturedKey()
    {
        RecordingLogger logger = new();
        OrderItem item = new(123, 456, 1);

        logger.Trace($"Item: {item:<@Item>SomeFormatText}");

        // OrderItem doesn't implement IFormattable, so the trailing format text has no visible
        // effect on its own rendering - but the message no longer goes through destructured
        // rendering at all now that a format follows the tag, which is the observable part: it
        // falls back to OrderItem's own record-generated ToString() ("=", not the destructuring
        // renderer's ":") instead of LogPropertyDestructuring's output.
        logger.LastMessage.Should().Be("Item: OrderItem { CartId = 123, ItemId = 456, Quantity = 1 }");
        KeyValuePair<string, object?> property = logger.LastProperties.Should().ContainSingle().Which;
        property.Key.Should().Be("@Item");
        property.Value.Should().BeSameAs(item);
    }

    [Fact]
    public void TaggedDestructuringFormat_WithTrailingNumericFormat_FormatsMessageInsteadOfDestructuring()
    {
        RecordingLogger logger = new();
        double amount = 3.14159;

        logger.Information($"Total: {amount:<@Amount>F3}");

        logger.LastMessage.Should().Be("Total: 3.142");
        KeyValuePair<string, object?> property = logger.LastProperties.Should().ContainSingle().Which;
        property.Key.Should().Be("@Amount");
        property.Value.Should().Be(amount);
    }

    [Fact]
    public void EmptyDestructuringTag_WithTrailingFormat_BehavesLikeEmptyTag_NoCapture()
    {
        RecordingLogger logger = new();
        double amount = 3.14159;

        logger.Information($"Total: {amount:<@>F3}");

        logger.LastMessage.Should().Be("Total: 3.142");
        logger.LastProperties.Should().BeEmpty();
    }

    [Fact]
    public void TaggedDestructuringFormat_WithAlignmentAndTrailingFormat_HonorsFormatForMessage()
    {
        RecordingLogger logger = new();
        double amount = 3.14159;

        logger.Information($"Total: {amount,8:<@Amount>F3}");

        logger.LastMessage.Should().Be("Total:    3.142");
        logger.LastProperties.Should().ContainSingle().Which.Key.Should().Be("@Amount");
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

    [Fact]
    public void TaggedDestructuringFormat_ValueType_CapturesRawValue_AndRendersDestructuredMessage()
    {
        RecordingLogger logger = new();
        Coordinates value = new(3, 4);

        // A value type that is both captured and destructure-rendered is boxed once and the same box
        // used for both, rather than converted to object separately for each. The captured property has
        // to stay the raw value - equal to the original, not the rendered text.
        logger.Trace($"At: {value:<@Position>}");

        logger.LastMessage.Should().Be("At: Coordinates { X: 3, Y: 4 }");
        KeyValuePair<string, object?> property = logger.LastProperties.Should().ContainSingle().Which;
        property.Key.Should().Be("@Position");
        property.Value.Should().Be(value);
    }

    [Fact]
    public void TaggedDestructuringFormat_ValueTypeScalar_CapturesRawValue()
    {
        RecordingLogger logger = new();
        decimal amount = 19.95m;

        logger.Trace($"Total: {amount:<@Amount>}");

        logger.LastMessage.Should().Be("Total: 19.95");
        KeyValuePair<string, object?> property = logger.LastProperties.Should().ContainSingle().Which;
        property.Key.Should().Be("@Amount");
        property.Value.Should().Be(amount);
        property.Value.Should().BeOfType<decimal>();
    }

    private readonly record struct Coordinates(int X, int Y);
}
