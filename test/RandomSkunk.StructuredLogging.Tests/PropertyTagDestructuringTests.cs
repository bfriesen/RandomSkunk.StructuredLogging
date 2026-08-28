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

    [Fact]
    public void SharedReferenceInSiblingBranches_RendersFullyInBoth()
    {
        RecordingLogger logger = new();
        Node shared = new() { Name = "shared" };
        Fork fork = new() { Left = shared, Right = shared };

        logger.Trace($"Fork: {fork:<@>}");

        // The same instance appearing twice is not a cycle - cycle tracking is ancestor-stack scoped, so
        // the second branch must still render in full rather than reporting a circular reference.
        logger.LastMessage.Should().Be(
            "Fork: Fork { Left: Node { Name: \"shared\", Next: null }, Right: Node { Name: \"shared\", Next: null } }");
    }

    [Fact]
    public void RepeatedRenders_ProduceIdenticalOutput()
    {
        RecordingLogger logger = new();
        Node node = new() { Name = "A", Next = new Node { Name = "B" } };

        logger.Trace($"Node: {node:<@>}");
        string first = logger.LastMessage!;

        // The scratch buffer and the ancestor stack are thread-static and reused across renders; anything
        // left behind in either would show up as drift here.
        for (int i = 0; i < 5; i++)
        {
            logger.Trace($"Node: {node:<@>}");
            logger.LastMessage.Should().Be(first);
        }
    }

    [Fact]
    public void RenderAfterCircularReference_IsUnaffected()
    {
        RecordingLogger logger = new();
        Node cyclic = new() { Name = "A" };
        cyclic.Next = cyclic;
        Node clean = new() { Name = "B" };

        logger.Trace($"Node: {cyclic:<@>}");
        logger.Trace($"Node: {clean:<@>}");

        // Bailing out of a cycle unwinds through a different path than a normal return - the reused
        // ancestor stack still has to come back empty for the next render.
        logger.LastMessage.Should().Be("Node: Node { Name: \"B\", Next: null }");
    }

    [Fact]
    public void PropertyGetterLogsWhileRendering_BothRendersAreCorrect()
    {
        RecordingLogger outer = new();
        RecordingLogger inner = new();

        // The scratch buffer and ancestor stack are thread-static, so a getter that logs a destructured
        // value reenters the renderer on the same thread while the outer render is still building. The
        // nested render has to get its own scratch state rather than scribbling into the outer one's.
        LogsWhileRendering value = new(inner);

        outer.Trace($"Outer: {value:<@>}");

        inner.LastMessage.Should().Be("Inner: Node { Name: \"nested\", Next: null }");
        outer.LastMessage.Should().Be("Outer: LogsWhileRendering { Nested: 99 }");
    }

    [Fact]
    public void ConcurrentRenders_DoNotShareScratchBuffers()
    {
        Node node = new() { Name = "A", Next = new Node { Name = "B" } };
        string[] results = new string[2000];

        Parallel.For(0, results.Length, i =>
        {
            RecordingLogger logger = new();
            logger.Trace($"Node: {node:<@>}");
            results[i] = logger.LastMessage!;
        });

        results.Distinct().Should().ContainSingle();
    }

    private sealed class Node
    {
        public string Name { get; set; } = "";

        public Node? Next { get; set; }
    }

    private sealed class Fork
    {
        public Node? Left { get; set; }

        public Node? Right { get; set; }
    }

    private sealed class LogsWhileRendering(RecordingLogger inner)
    {
        public int Nested
        {
            get
            {
                inner.Trace($"Inner: {new Node { Name = "nested" }:<@>}");
                return 99;
            }
        }
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
