using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class AvoidLoggerExtensionsCodeFixProviderTests
{
    [Theory]
    [InlineData(
        """logger.LogInformation("User {UserId} logged in from {IpAddress}", userId, ipAddress);""",
        """logger.Information($"User {userId:<UserId>} logged in from {ipAddress:<IpAddress>}");""")]
    [InlineData(
        """logger.LogTrace("no properties here");""",
        """logger.Trace("no properties here");""")]
    [InlineData(
        """logger.LogDebug("one {Property}", value);""",
        """logger.Debug($"one {value:<Property>}");""")]
    [InlineData(
        """logger.LogWarning(new EventId(1, "Name"), "Slow request {Path}", path);""",
        """logger.Warning(new EventId(1, "Name"), $"Slow request {path:<Path>}");""")]
    [InlineData(
        """logger.LogError(new Exception(), "Failed {OrderId}", orderId);""",
        """logger.Error(new Exception(), $"Failed {orderId:<OrderId>}");""")]
    [InlineData(
        """logger.LogCritical(new EventId(1, "Name"), new Exception(), "Fatal {Code}", code);""",
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Fatal {code:<Code>}");""")]
    [InlineData(
        """logger.Log(LogLevel.Information, "User {UserId} logged in", userId);""",
        """logger.Write(LogLevel.Information, $"User {userId:<UserId>} logged in");""")]
    [InlineData(
        """LoggerExtensions.LogInformation(logger, "User {UserId}", userId);""",
        """logger.Information($"User {userId:<UserId>}");""")]
    [InlineData(
        """logger.LogInformation("Weather forecast data generated successfully. {@WeatherForecast}", value);""",
        """logger.Information($"Weather forecast data generated successfully. {value:<@WeatherForecast>}");""")]
    [InlineData(
        """logger.LogInformation("Raw value {$Value}", value);""",
        """logger.Information($"Raw value {value:<$Value>}");""")]
    public async Task LiteralMessageWithMatchingArgs_IsFixed(string call, string expectedCall)
    {
        string source = TestSource.WrapInMethodBody(call);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(source, new AvoidLoggerExtensionsAnalyzer(), new AvoidLoggerExtensionsCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }

    [Fact]
    public async Task MismatchedPlaceholderCount_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.LogInformation("User {UserId} did {Action}", userId);""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(source, new AvoidLoggerExtensionsAnalyzer(), new AvoidLoggerExtensionsCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonLiteralMessage_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody("logger.LogInformation(GetMessage());");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(source, new AvoidLoggerExtensionsAnalyzer(), new AvoidLoggerExtensionsCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task ExplicitArgsArray_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.LogInformation("User {UserId}", new object[] { userId });""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(source, new AvoidLoggerExtensionsAnalyzer(), new AvoidLoggerExtensionsCodeFixProvider());

        fixedSource.Should().BeNull();
    }
}
