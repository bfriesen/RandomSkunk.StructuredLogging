using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class InterpolatedStringLocalMessageAnalyzerTests
{
    [Theory]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Trace(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Debug(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Information(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Warning(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Error(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Critical(msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Write(LogLevel.Information, msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Debug(new EventId(1, "Name"), msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Debug(new Exception(), msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Debug(new EventId(1, "Name"), new Exception(), msg);
        """)]
    [InlineData(
        """
        string msg = $"User {userId}";
        logger.Debug(msg, ("UserId", userId));
        """)]
    public async Task InterpolatedStringLocalPassedAsMessage_IsFlagged(string statement)
    {
        string source = TestSource.WrapInMethodBody(statement);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0007");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("msg");
    }

    [Fact]
    public async Task InterpolatedStringLiteralPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"User {userId}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task PlainStringLiteralLocal_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            string msg = "constant message";
            logger.Debug(msg);
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringParameterPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.Debug(dynamicName);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InterpolatedStringLocalPassedToLoggerExtensionsMethod_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            string msg = $"User {userId}";
            logger.LogInformation(msg);
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleInterpolatedStringLocals_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            string first = $"first {userId}";
            string second = $"second {userId}";
            logger.Debug(first);
            logger.Information(second);
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task NoStructuredLoggerExtensionsCalls_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.IsEnabled(LogLevel.Information);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringLocalMessageAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
