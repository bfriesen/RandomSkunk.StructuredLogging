using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class LogPropertyTupleArgumentCodeFixProviderTests
{
    [Fact]
    public async Task EmptyHole_IsFilledWithTheTupleValue()
    {
        var source = TestSource.WrapInMethodBody("""logger.Trace($"Hello, {}!", ("Who", who));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTupleArgumentAnalyzer(), new LogPropertyTupleArgumentCodeFixProvider(),
            allowCompilerErrors: true);

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.Trace($"Hello, {who:<Who>}!");"""));
    }

    [Theory]
    [InlineData(
        """logger.Trace($"Hello, there!", ("Who", who));""",
        """logger.Trace($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Debug($"Hello, there!", ("Who", who));""",
        """logger.Debug($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Information($"Hello, there!", ("Who", who));""",
        """logger.Information($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Warning($"Hello, there!", ("Who", who));""",
        """logger.Warning($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Error($"Hello, there!", ("Who", who));""",
        """logger.Error($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Critical($"Hello, there!", ("Who", who));""",
        """logger.Critical($"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Write(LogLevel.Information, $"Hello, there!", ("Who", who));""",
        """logger.Write(LogLevel.Information, $"Hello, there! {who:<Who>}");""")]
    [InlineData(
        """logger.Debug($"", ("UserId", userId));""",
        """logger.Debug($" {userId:<UserId>}");""")]
    [InlineData(
        """logger.Warning(new EventId(1, "Name"), $"Slow request", ("Path", path));""",
        """logger.Warning(new EventId(1, "Name"), $"Slow request {path:<Path>}");""")]
    [InlineData(
        """logger.Error(new Exception(), $"Failed", ("OrderId", orderId));""",
        """logger.Error(new Exception(), $"Failed {orderId:<OrderId>}");""")]
    [InlineData(
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Fatal", ("Code", code));""",
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Fatal {code:<Code>}");""")]
    public async Task NoEmptyHole_AppendsANewHoleToTheEnd(string call, string expectedCall)
    {
        var source = TestSource.WrapInMethodBody(call);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTupleArgumentAnalyzer(), new LogPropertyTupleArgumentCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }

    [Fact]
    public async Task PlainStringLiteralMessage_WithEmptyHoleMarker_IsPromotedAndFilled()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("Hello, {}!", ("Who", who));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTupleArgumentAnalyzer(), new LogPropertyTupleArgumentCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who:<Who>}!");"""));
    }

    [Fact]
    public async Task PlainStringLiteralMessage_WithoutEmptyHoleMarker_IsPromotedAndAppended()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("Hello, there!", ("Who", who));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTupleArgumentAnalyzer(), new LogPropertyTupleArgumentCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.Debug($"Hello, there! {who:<Who>}");"""));
    }

    [Fact]
    public async Task ConstantNonLiteralPropertyName_UsesItsFoldedValue()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, there!", ("User" + "Id", who));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTupleArgumentAnalyzer(), new LogPropertyTupleArgumentCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.Debug($"Hello, there! {who:<UserId>}");"""));
    }
}
