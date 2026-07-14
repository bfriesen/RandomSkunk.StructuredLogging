using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class RemoveLogPropertyTagFormatCodeFixProviderTests
{
    [Theory]
    [InlineData(
        """logger.Trace($"Hello, {who:<Recipient>}!");""",
        """logger.Trace($"Hello, {who}!");""")]
    [InlineData(
        """logger.Debug($"[{ts:<Timestamp>HH:mm:ss}]");""",
        """logger.Debug($"[{ts:HH:mm:ss}]");""")]
    [InlineData(
        """logger.Warning(new EventId(1, "Name"), $"Slow request {path:<Path>N2}");""",
        """logger.Warning(new EventId(1, "Name"), $"Slow request {path:N2}");""")]
    [InlineData(
        """logger.Error(new Exception(), $"Failed {orderId:<OrderId>}");""",
        """logger.Error(new Exception(), $"Failed {orderId}");""")]
    [InlineData(
        """logger.Debug($"Order {orderId:<OrderId>}", ("Total", value));""",
        """logger.Debug($"Order {orderId}", ("Total", value));""")]
    [InlineData(
        """logger.Debug($"Item: {value:<@Value>}");""",
        """logger.Debug($"Item: {value:<@>}");""")]
    [InlineData(
        """logger.Debug($"Item: {value:<@Value>N2}");""",
        """logger.Debug($"Item: {value:<@>N2}");""")]
    public async Task TagFormatHole_HasTagRemoved(string call, string expectedCall)
    {
        var source = TestSource.WrapInMethodBody(call);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTagFormatAnalyzer(), new LogPropertyTagFormatCodeFixProvider(),
            action => action.Title.StartsWith("Remove", StringComparison.Ordinal));

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }
}
