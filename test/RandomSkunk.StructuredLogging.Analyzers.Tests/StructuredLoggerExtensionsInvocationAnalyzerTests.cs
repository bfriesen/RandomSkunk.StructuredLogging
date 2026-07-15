using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class StructuredLoggerExtensionsInvocationAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"message");""", "Trace")]
    [InlineData("""logger.Debug($"message");""", "Debug")]
    [InlineData("""logger.Information($"message");""", "Information")]
    [InlineData("""logger.Warning($"message");""", "Warning")]
    [InlineData("""logger.Error($"message");""", "Error")]
    [InlineData("""logger.Critical($"message");""", "Critical")]
    [InlineData("""logger.Write(LogLevel.Information, $"message");""", "Write")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), $"message");""", "Debug")]
    [InlineData("""logger.Debug(new Exception(), $"message");""", "Debug")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), new Exception(), $"message");""", "Debug")]
    [InlineData("""logger.Debug($"message", ("UserId", userId));""", "Debug")]
    [InlineData("""logger.Debug($"message", ("UserId", userId), ("IpAddress", ipAddress));""", "Debug")]
    [InlineData("""StructuredLoggerExtensions.Debug(logger, $"message");""", "Debug")]
    public async Task StructuredLoggerExtensionsMethodCall_IsFlagged(string call, string methodName)
    {
        var source = TestSource.WrapInMethodBody(call);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0005");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Hidden);
        diagnostic.GetMessage().Should().Contain(methodName);
    }

    [Fact]
    public async Task MultipleStructuredLoggerExtensionsCalls_AreAllFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """
            logger.Debug($"first");
            logger.Information($"second");
            """);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoggerExtensionsMethodCall_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.LogInformation("message");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InterfaceLogMethodCall_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Log(LogLevel.Information, new EventId(1, "Name"), "state", null, (state, exception) => state);""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UnrelatedExtensionMethodWithSameName_IsNotFlagged()
    {
        var source = """
            using Microsoft.Extensions.Logging;

            namespace TestNamespace;

            public static class NotStructuredLoggerExtensions
            {
                public static void Debug(this ILogger logger, string message) { }
            }

            public class TestClass
            {
                public void TestMethod(ILogger logger)
                {
                    logger.Debug("message");
                }
            }
            """;

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoStructuredLoggerExtensionsCalls_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("logger.IsEnabled(LogLevel.Information);");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new StructuredLoggerExtensionsInvocationAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
