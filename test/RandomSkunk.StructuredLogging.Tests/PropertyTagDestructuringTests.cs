using System.Globalization;
using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Tests;

public class PropertyTagDestructuringTests
{
    [Theory]
    [InlineData(42, "42")]
    [InlineData(3.14, "3.14")]
    [InlineData(true, "True")]
    public void NumericOrBoolScalar_RendersUnquoted(object value, string expected)
    {
        RecordingLogger logger = new();

        logger.Trace($"Value: {value:<@>}");

        logger.LastMessage.Should().Be($"Value: {expected}");
    }

    [Fact]
    public void EnumScalar_RendersUnquoted()
    {
        RecordingLogger logger = new();

        logger.Trace($"Day: {DayOfWeek.Monday:<@>}");

        logger.LastMessage.Should().Be("Day: Monday");
    }

    [Fact]
    public void GuidScalar_RendersUnquoted()
    {
        RecordingLogger logger = new();
        Guid value = Guid.Parse("11111111-2222-3333-4444-555555555555");

        logger.Trace($"Id: {value:<@>}");

        logger.LastMessage.Should().Be($"Id: {value}");
    }

    [Fact]
    public void DateTimeScalar_RendersUnquoted_UsingInvariantCulture()
    {
        RecordingLogger logger = new();
        DateTime value = new(2024, 1, 2, 13, 14, 15, DateTimeKind.Utc);

        logger.Trace($"When: {value:<@>}");

        logger.LastMessage.Should().Be($"When: {value.ToString(null, CultureInfo.InvariantCulture)}");
    }

    [Fact]
    public void StringScalar_RendersQuoted_WithEscaping()
    {
        RecordingLogger logger = new();
        string value = "hello \"world\"";

        logger.Trace($"Value: {value:<@>}");

        logger.LastMessage.Should().Be("Value: \"hello \\\"world\\\"\"");
    }

    [Fact]
    public void CharScalar_RendersQuoted()
    {
        RecordingLogger logger = new();

        logger.Trace($"Value: {'x':<@>}");

        logger.LastMessage.Should().Be("Value: 'x'");
    }

    [Fact]
    public void NullValue_RendersNullLiteral()
    {
        RecordingLogger logger = new();
        OrderItem? item = null;

        logger.Trace($"Value: {item:<@>}");

        logger.LastMessage.Should().Be("Value: null");
    }

    [Fact]
    public void SimpleObject_RendersTypeNameAndProperties_MatchesPromptExample()
    {
        RecordingLogger logger = new();
        OrderItem item = new(123, 456, 1);

        logger.Trace($"Item added to cart: {item:<@Item>}");

        logger.LastMessage.Should().Be("Item added to cart: OrderItem { CartId: 123, ItemId: 456, Quantity: 1 }");
    }

    [Fact]
    public void NestedObject_RendersRecursively()
    {
        RecordingLogger logger = new();
        Customer customer = new("Alice", new Address("Springfield", "IL"));

        logger.Trace($"Customer: {customer:<@>}");

        logger.LastMessage.Should().Be(
            "Customer: Customer { Name: \"Alice\", Address: Address { City: \"Springfield\", State: \"IL\" } }");
    }

    [Fact]
    public void Collection_RendersAsBracketList()
    {
        RecordingLogger logger = new();
        List<int> values = new() { 1, 2, 3 };

        logger.Trace($"Values: {values:<@>}");

        logger.LastMessage.Should().Be("Values: [1, 2, 3]");
    }

    [Fact]
    public void Dictionary_RendersAsKeyValueBraces()
    {
        RecordingLogger logger = new();
        Dictionary<string, int> values = new() { ["A"] = 1, ["B"] = 2 };

        logger.Trace($"Values: {values:<@>}");

        logger.LastMessage.Should().Be("Values: { [\"A\"]: 1, [\"B\"]: 2 }");
    }

    [Fact]
    public void AnonymousType_OmitsTypeName()
    {
        RecordingLogger logger = new();
        var value = new { Foo = 1, Bar = "x" };

        logger.Trace($"Value: {value:<@>}");

        logger.LastMessage.Should().Be("Value: { Foo: 1, Bar: \"x\" }");
    }

    [Fact]
    public void CircularReference_RendersPlaceholder_DoesNotStackOverflow()
    {
        RecordingLogger logger = new();
        Node node = new() { Name = "A" };
        node.Next = node;

        logger.Trace($"Node: {node:<@>}");

        logger.LastMessage.Should().Be("Node: Node { Name: \"A\", Next: <circular reference> }");
    }

    [Fact]
    public void DeepNesting_StopsAtMaxDepth()
    {
        RecordingLogger logger = new();
        Node? head = null;
        for (int i = 0; i < 15; i++)
            head = new Node { Name = i.ToString(), Next = head };

        logger.Trace($"Node: {head:<@>}");

        logger.LastMessage.Should().Contain("...");
    }

    [Fact]
    public void LargeCollection_CapsAtTenItems()
    {
        RecordingLogger logger = new();
        List<int> values = Enumerable.Range(1, 15).ToList();

        logger.Trace($"Values: {values:<@>}");

        logger.LastMessage.Should().Be("Values: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]");
    }

    [Fact]
    public void PropertyGetterThrows_DoesNotThrow_RendersPlaceholder()
    {
        RecordingLogger logger = new();
        ThrowingProperty value = new();

        logger.Trace($"Value: {value:<@>}");

        logger.LastMessage.Should().Be("Value: ThrowingProperty { Good: 1, Bad: <getter threw InvalidOperationException> }");
    }

    [Fact]
    public void StructValue_RendersAsObject()
    {
        RecordingLogger logger = new();
        Point value = new(1, 2);

        logger.Trace($"Point: {value:<@>}");

        logger.LastMessage.Should().Be("Point: Point { X: 1, Y: 2 }");
    }

    private sealed class Node
    {
        public string Name { get; set; } = "";

        public Node? Next { get; set; }
    }

    private sealed class ThrowingProperty
    {
        public int Good { get; set; } = 1;

        public int Bad => throw new InvalidOperationException("boom");
    }

    private readonly record struct Point(int X, int Y);

    private sealed record Address(string City, string State);

    private sealed record Customer(string Name, Address Address);
}

internal sealed record OrderItem(int CartId, int ItemId, int Quantity);
