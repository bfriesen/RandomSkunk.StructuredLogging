using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class InterpolatedStringHelperMethodArgumentAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Debug(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Information(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Warning(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Error(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Critical(FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Write(LogLevel.Information, FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Debug(new Exception(), FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), new Exception(), FormatMessage($"User {userId}"));""")]
    [InlineData("""logger.Debug(FormatMessage($"User {userId}"), ("UserId", userId));""")]
    public async Task InterpolatedStringLiteralPassedToHelperMethod_IsFlagged(string statement)
    {
        string source = TestSource.WrapInMethodBody(statement);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0010");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("FormatMessage");
    }

    [Fact]
    public async Task InterpolatedStringLiteralPassedThroughNestedHelperMethods_IsFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug(FormatMessage(FormatMessage($"User {userId}")));""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0010");
        diagnostic.GetMessage().Should().Contain("FormatMessage");
    }

    [Fact]
    public async Task InterpolatedStringLiteralPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"User {userId}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task PlainStringLiteralPassedToHelperMethod_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug(FormatMessage("constant message"));""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InterpolatedStringLocalPassedToHelperMethod_IsNotFlagged()
    {
        // The literal is behind a local variable, not passed to the helper directly - this
        // analyzer only looks for the literal passed straight in as an argument.
        string source = TestSource.WrapInMethodBody(
            """
            string msg = $"User {userId}";
            logger.Debug(FormatMessage(msg));
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringParameterPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.Debug(dynamicName);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InterpolatedStringLiteralPassedToHelperMethodOfLoggerExtensionsCall_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.LogInformation(FormatMessage($"User {userId}"));""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleFlaggedCalls_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            logger.Debug(FormatMessage($"first {userId}"));
            logger.Information(FormatMessage($"second {userId}"));
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task NoStructuredLoggerExtensionsCalls_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.IsEnabled(LogLevel.Information);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringHelperMethodArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
