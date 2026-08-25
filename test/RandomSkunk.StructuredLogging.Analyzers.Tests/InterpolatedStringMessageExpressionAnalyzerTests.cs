using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class InterpolatedStringMessageExpressionAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Debug($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Information($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Warning($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Error($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Critical($"User {userId}".ToUpper());""")]
    [InlineData("""logger.Write(LogLevel.Information, $"User {userId}".ToUpper());""")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), $"User {userId}".ToUpper());""")]
    [InlineData("""logger.Debug(new Exception(), $"User {userId}".ToUpper());""")]
    [InlineData("""logger.Debug(new EventId(1, "Name"), new Exception(), $"User {userId}".ToUpper());""")]
    [InlineData("""logger.Debug($"User {userId}".ToUpper(), ("UserId", userId));""")]
    public async Task MethodCallAppliedToInterpolatedStringLiteral_IsFlagged(string statement)
    {
        string source = TestSource.WrapInMethodBody(statement);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0009");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("ToUpper");
    }

    [Fact]
    public async Task ChainedMethodCallAppliedToInterpolatedStringLiteral_IsFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"User {userId}".ToUpper().Trim());""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0009");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("Trim");
    }

    [Theory]
    [InlineData("""logger.Debug($"count: " + userId);""")]
    [InlineData("""logger.Debug(userId + $"count: ");""")]
    [InlineData("""logger.Debug("prefix " + $"count: {userId}" + " suffix");""")]
    public async Task ConcatenationAppliedToInterpolatedStringLiteral_IsFlagged(string statement)
    {
        string source = TestSource.WrapInMethodBody(statement);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0009");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("concatenation");
    }

    [Fact]
    public async Task InterpolatedStringLiteralPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"User {userId}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task PlainStringLiteralMethodCall_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug("constant message".ToUpper());""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InterpolatedStringLocalMethodCall_IsNotFlagged()
    {
        // The literal is behind a local variable, not applied to directly - this analyzer only
        // looks for an operation applied straight to the interpolated string literal itself.
        string source = TestSource.WrapInMethodBody(
            """
            string msg = $"User {userId}";
            logger.Debug(msg.ToUpper());
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringParameterPassedDirectly_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.Debug(dynamicName);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MethodCallOnInterpolatedStringLiteralPassedToLoggerExtensionsMethod_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.LogInformation($"User {userId}".ToUpper());""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleFlaggedCalls_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            logger.Debug($"first {userId}".ToUpper());
            logger.Information($"second {userId}" + " suffix");
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task NoStructuredLoggerExtensionsCalls_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("logger.IsEnabled(LogLevel.Information);");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new InterpolatedStringMessageExpressionAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
