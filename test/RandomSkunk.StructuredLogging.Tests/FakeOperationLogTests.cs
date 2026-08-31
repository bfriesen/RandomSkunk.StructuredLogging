using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NSubstitute;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

/// <summary>
/// Covers <see cref="FakeOperationLog"/>, whose whole reason for existing is that a proxy-generating
/// mocking framework can't forward the two members taking an <see cref="OperationLogInterpolatedStringHandler"/> -
/// invoking either one on a mock of <see cref="IOperationLog"/> or <see cref="ISubOperationLog"/> itself
/// throws <see cref="InvalidProgramException"/>. So these tests drive real frameworks (Moq and NSubstitute)
/// rather than a hand-rolled subclass alone: a plain subclass would never exercise the proxy generation
/// that made the fake necessary.
/// </summary>
public class FakeOperationLogTests
{
    /// <summary>
    /// Stands in for the code under test: it takes the interface, and it calls the interpolated
    /// overloads - the exact shape that fails against a mock of <see cref="IOperationLog"/>.
    /// </summary>
    private static void RunOperation(IOperationLog log, int userId)
    {
        log.Append($"Fetching user {userId}");

        using ISubOperationLog subLog = log.BeginSubOperation($"Load {userId}");
        subLog.Append($"loaded {userId}");
    }

    [Fact]
    public void Moq_InterpolatedAppend_ArrivesAtAppend()
    {
        Mock<FakeOperationLog> log = new() { CallBase = true };

        // BeginSubOperation is abstract - it has no do-nothing default, so the sub-operation it returns
        // has to be configured explicitly. Returning the root itself reproduces the old default behavior.
        log.Setup(x => x.BeginSubOperation(It.IsAny<string>())).Returns(log.Object);

        RunOperation(log.Object, 42);

        log.Verify(x => x.Append("Fetching user 42"), Times.Once);
        log.Verify(x => x.BeginSubOperation("Load 42"), Times.Once);
        log.Verify(x => x.Append("loaded 42"), Times.Once);
    }

    [Fact]
    public void NSubstitute_InterpolatedAppend_ArrivesAtAppend()
    {
        FakeOperationLog log = Substitute.ForPartsOf<FakeOperationLog>();

        // BeginSubOperation is abstract - it has no do-nothing default, so the sub-operation it returns
        // has to be configured explicitly. Left unconfigured, NSubstitute would auto-generate a raw
        // ISubOperationLog substitute to return, and calling its interpolated Append would throw the exact
        // InvalidProgramException this fake exists to avoid.
        log.BeginSubOperation(Arg.Any<string>()).Returns(log);

        RunOperation(log, 42);

        log.Received(1).Append("Fetching user 42");
        log.Received(1).BeginSubOperation("Load 42");
        log.Received(1).Append("loaded 42");
    }

    [Fact]
    public void Moq_Dispose_IsVerifiable()
    {
        Mock<FakeOperationLog> log = new() { CallBase = true };

        using (IOperationLog scoped = log.Object)
            scoped.Append($"work");

        log.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void SubOperation_IsTheSameInstanceAsTheRoot()
    {
        // BeginSubOperation is abstract, so it has to be configured to return something - here, the root
        // itself, so disposing the sub-operation and disposing the root are the same call.
        Mock<FakeOperationLog> log = new() { CallBase = true };
        log.Setup(x => x.BeginSubOperation("Load")).Returns(log.Object);

        using (ISubOperationLog subLog = ((IOperationLog)log.Object).BeginSubOperation("Load"))
            subLog.Should().BeSameAs(log.Object);

        log.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void SubclassWithoutAMockingFramework_SeesInterpolatedText()
    {
        RecordingFake log = new();

        RunOperation(log, 99);

        log.AppendedText.Should().Equal("Fetching user 99", "loaded 99");
        log.SubOperationNames.Should().Equal("Load 99");
    }

    [Fact]
    public void SuccessiveInterpolatedCalls_DoNotAccumulate()
    {
        // The interpolated-handler overloads clear the buffer after draining it, so the second call sees
        // only its own text - not the first call's still sitting in front of it.
        RecordingFake log = new();

        IOperationLog asInterface = log;
        asInterface.Append($"first {1}");
        asInterface.Append($"second {2}");

        log.AppendedText.Should().Equal("first 1", "second 2");
    }

    [Fact]
    public void PlainStringAppend_RoutesThroughTheSameVirtualMember()
    {
        // Append(string) on either interface arrives at the same virtual Append(string) hook.
        Mock<FakeOperationLog> log = new() { CallBase = true };
        string message = "already a string";

        ((IOperationLog)log.Object).Append(message);

        log.Verify(x => x.Append(message), Times.Once);
    }

    [Fact]
    public void IsEnabled_IsAlwaysTrue()
    {
        // IsEnabled has no public counterpart at all - both explicit implementations just return true
        // unconditionally - so this fake has no disabled state, and interpolated Append/BeginSubOperation
        // overloads always evaluate their holes regardless of CallBase or setup. FakeOperationLog is
        // abstract (BeginSubOperation has no sensible do-nothing default), so any concrete subclass will
        // do here - RecordingFake is already at hand.
        FakeOperationLog log = new RecordingFake();
        bool holeEvaluated = false;

        IOperationLog asInterface = log;
        ISubOperationLog asSubInterface = log;
        asInterface.Append($"{Evaluate()}");

        asInterface.IsEnabled.Should().BeTrue();
        asSubInterface.IsEnabled.Should().BeTrue();
        holeEvaluated.Should().BeTrue();

        int Evaluate()
        {
            holeEvaluated = true;
            return 1;
        }
    }

    [Fact]
    public void DefaultMembers_ReturnSelfAndEmptyState()
    {
        // BeginSubOperation is abstract - RecordingFake's override happens to return `this`, which is what
        // the assertion below relies on.
        FakeOperationLog log = new RecordingFake();
        IOperationLog asInterface = log;

        asInterface.AddProperty("A", 1).Should().BeSameAs(log);
        asInterface.Escalate(LogLevel.Error).Should().BeSameAs(log);
        asInterface.AppendException(new InvalidOperationException()).Should().BeSameAs(log);
        asInterface.AppendResult(1).Should().BeSameAs(log);
        asInterface.SetException(new InvalidOperationException()).Should().BeSameAs(log);
        asInterface.SetResult(1).Should().BeSameAs(log);
        asInterface.Append("text").Should().BeSameAs(log);
        asInterface.AppendValue(1).Should().BeSameAs(log);
        asInterface.BeginSubOperation("Nested").Should().BeSameAs(log);

        asInterface.Properties.Should().Equal([new KeyValuePair<string, object?>("A", 1)]);
        log.EventId.Should().Be(default(EventId));
        asInterface.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SubOperation_DefaultMembers_ReturnSelfAndEmptyState()
    {
        // BeginSubOperation is abstract - RecordingFake's override happens to return `this`, which is what
        // the assertion below relies on.
        FakeOperationLog log = new RecordingFake();
        ISubOperationLog subLog = log;

        subLog.AddProperty("A", 1).Should().BeSameAs(log);
        subLog.Escalate(LogLevel.Error).Should().BeSameAs(log);
        subLog.AppendException(new InvalidOperationException()).Should().BeSameAs(log);
        subLog.AppendResult(1).Should().BeSameAs(log);
        subLog.Append("text").Should().BeSameAs(log);
        subLog.AppendValue(1).Should().BeSameAs(log);
        subLog.BeginSubOperation("Nested").Should().BeSameAs(log);

        subLog.Properties.Should().Equal([new KeyValuePair<string, object?>("A", 1)]);
        log.EventId.Should().Be(default(EventId));
        subLog.IsEnabled.Should().BeTrue();
    }

    private sealed class RecordingFake : FakeOperationLog
    {
        public List<string> AppendedText { get; } = new();

        public List<string> SubOperationNames { get; } = new();

        public override void Append(string text)
        {
            AppendedText.Add(text);
        }

        public override ISubOperationLog BeginSubOperation(string operationName)
        {
            SubOperationNames.Add(operationName);
            return this;
        }
    }
}
