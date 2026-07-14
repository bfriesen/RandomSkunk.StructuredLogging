using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class NonCapturingInterpolationHoleAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Debug($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Information($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Warning($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Error($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Critical($"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Write(LogLevel.Information, $"Hello, {who}!");""", "{who}")]
    [InlineData("""logger.Debug($"Value: {who:N2}");""", "{who:N2}")]
    public async Task PlainOrNonTagFormatHole_IsFlagged(string call, string expectedText)
    {
        var source = TestSource.WrapInMethodBody(call);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0003");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Hidden);
        diagnostic.GetMessage().Should().Contain("who");

        var syntaxTree = diagnostic.Location.SourceTree!;
        var text = syntaxTree.GetText().ToString(diagnostic.Location.SourceSpan);
        text.Should().Be(expectedText);
    }

    [Fact]
    public async Task EmptyTag_OptsOutAndIsFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:<>N2}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().ContainSingle();
        diagnostics[0].Id.Should().Be("RSSL0003");
    }

    [Fact]
    public async Task MultipleNonCapturingHoles_AreAllFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"User {userId} from {ipAddress}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("userId"));
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("ipAddress"));
    }

    [Fact]
    public async Task MixOfCapturingAndNonCapturingHoles_OnlyNonCapturingIsFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"User {userId:<UserId>} from {ipAddress}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("ipAddress");
    }

    [Fact]
    public async Task TagFormatHole_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who:<Recipient>}!");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonCapturingHoleInPlainStringInterpolation_IsNotFlagged()
    {
        // This interpolated string is converted to `string`, not to one of
        // RandomSkunk.StructuredLogging's interpolated string handler types, because MEL's
        // LogInformation takes a plain `string message` parameter.
        var source = TestSource.WrapInMethodBody(
            """logger.LogInformation($"Hello, {who}!");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringLiteralMessage_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("no interpolation here");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new NonCapturingInterpolationHoleAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
