using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class LogPropertyTagFormatCodeFixProviderTests
{
    [Theory]
    [InlineData(
        """logger.Debug($"User logged in. {userName:<UserName>}");""",
        """logger.Debug($"User logged in.", ("UserName", userName));""")]
    [InlineData(
        """logger.Trace($"{userId:<UserId>} logged in");""",
        """logger.Trace($" logged in", ("UserId", userId));""")]
    [InlineData(
        """logger.Information($"{userId:<UserId>}");""",
        """logger.Information($"", ("UserId", userId));""")]
    [InlineData(
        """logger.Debug($"User {userId:<UserId>} logged in");""",
        """logger.Debug($"User logged in", ("UserId", userId));""")]
    [InlineData(
        """logger.Warning(new EventId(1, "Name"), $"Slow request {path:<Path>}");""",
        """logger.Warning(new EventId(1, "Name"), $"Slow request", ("Path", path));""")]
    [InlineData(
        """logger.Error(new Exception(), $"Failed {orderId:<OrderId>}");""",
        """logger.Error(new Exception(), $"Failed", ("OrderId", orderId));""")]
    [InlineData(
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Fatal {code:<Code>}");""",
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Fatal", ("Code", code));""")]
    [InlineData(
        """logger.Write(LogLevel.Information, $"Done {value:<Value>}");""",
        """logger.Write(LogLevel.Information, $"Done", ("Value", value));""")]
    [InlineData(
        """logger.Debug($"[{value:<Timestamp>HH:mm:ss}]");""",
        """logger.Debug($"[]", ("Timestamp", value));""")]
    [InlineData(
        """logger.Debug($"Order {orderId:<OrderId>}", ("Total", value));""",
        """logger.Debug($"Order", ("Total", value), ("OrderId", orderId));""")]
    [InlineData(
        """logger.Debug($"Hello {who:<Who>} how are you?");""",
        """logger.Debug($"Hello how are you?", ("Who", who));""")]
    [InlineData(
        """logger.Debug($"Item {value:<@Value>} added");""",
        """logger.Debug($"Item added", ("@Value", value));""")]
    public async Task TagFormatHole_IsExtractedToTrailingTupleArgument(string call, string expectedCall)
    {
        var source = TestSource.WrapInMethodBody(call);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new LogPropertyTagFormatAnalyzer(), new LogPropertyTagFormatCodeFixProvider(),
            action => action.Title.StartsWith("Move", StringComparison.Ordinal));

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }
}
