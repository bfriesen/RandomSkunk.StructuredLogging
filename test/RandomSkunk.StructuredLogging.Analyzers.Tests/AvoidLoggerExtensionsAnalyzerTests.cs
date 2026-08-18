using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class AvoidLoggerExtensionsAnalyzerTests
{
    [Theory]
    [InlineData("""logger.LogTrace("message");""", "LogTrace")]
    [InlineData("""logger.LogDebug("message");""", "LogDebug")]
    [InlineData("""logger.LogInformation("message");""", "LogInformation")]
    [InlineData("""logger.LogWarning("message");""", "LogWarning")]
    [InlineData("""logger.LogError("message");""", "LogError")]
    [InlineData("""logger.LogCritical("message");""", "LogCritical")]
    [InlineData("""logger.Log(LogLevel.Information, "message");""", "Log")]
    [InlineData("""logger.LogInformation(new EventId(1, "Name"), "message");""", "LogInformation")]
    [InlineData("""logger.LogError(new Exception(), "message");""", "LogError")]
    [InlineData("""logger.LogError(new EventId(1, "Name"), new Exception(), "message");""", "LogError")]
    [InlineData("""LoggerExtensions.LogInformation(logger, "message");""", "LogInformation")]
    public async Task LoggerExtensionsMethodCall_IsFlagged(string call, string methodName)
    {
        string source = TestSource.WrapInMethodBody(call);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new AvoidLoggerExtensionsAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostic.GetMessage().Should().Contain(methodName);
    }

    [Fact]
    public async Task InterfaceLogMethodCall_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Log(LogLevel.Information, new EventId(1, "Name"), "state", null, (state, exception) => state);""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new AvoidLoggerExtensionsAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UnrelatedExtensionMethodWithSameName_IsNotFlagged()
    {
        string source = """
            using Microsoft.Extensions.Logging;

            namespace TestNamespace;

            public static class NotLoggerExtensions
            {
                public static void LogInformation(this ILogger logger, string message) { }
            }

            public class TestClass
            {
                public void TestMethod(ILogger logger)
                {
                    logger.LogInformation("message");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new AvoidLoggerExtensionsAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoLoggerCalls_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.IsEnabled(LogLevel.Information);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new AvoidLoggerExtensionsAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
