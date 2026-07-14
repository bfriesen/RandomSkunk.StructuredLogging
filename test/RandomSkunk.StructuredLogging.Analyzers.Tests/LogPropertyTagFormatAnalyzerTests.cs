using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class LogPropertyTagFormatAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Debug($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Information($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Warning($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Error($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Critical($"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Write(LogLevel.Information, $"Hello, {who:<Recipient>}!");""", "{who:<Recipient>}", "Recipient")]
    [InlineData("""logger.Debug($"[{who:<Timestamp>HH:mm:ss}]");""", "{who:<Timestamp>HH:mm:ss}", "Timestamp")]
    [InlineData("""logger.Debug($"Hello, {who:<@Recipient>}!");""", "{who:<@Recipient>}", "Recipient")]
    [InlineData("""logger.Debug($"[{who:<@Timestamp>HH:mm:ss}]");""", "{who:<@Timestamp>HH:mm:ss}", "Timestamp")]
    public async Task TagFormatHole_IsFlagged(string call, string expectedText, string expectedPropertyName)
    {
        var source = TestSource.WrapInMethodBody(call);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0002");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Hidden);
        diagnostic.GetMessage().Should().Contain(expectedPropertyName);

        var syntaxTree = diagnostic.Location.SourceTree!;
        var text = syntaxTree.GetText().ToString(diagnostic.Location.SourceSpan);
        text.Should().Be(expectedText);
    }

    [Fact]
    public async Task MultipleTagFormatHoles_AreAllFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"User {userId:<UserId>} from {ipAddress:<IpAddress>}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("UserId"));
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("IpAddress"));
    }

    [Fact]
    public async Task PlainInterpolationHole_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonTagFormatHole_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:N2}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyTag_OptsOutAndIsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:<>N2}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DestructuringEmptyTag_OptsOutAndIsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:<@>N2}");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TagFormatHoleInPlainStringInterpolation_IsNotFlagged()
    {
        // This interpolated string is converted to `string`, not to one of
        // RandomSkunk.StructuredLogging's interpolated string handler types, because MEL's
        // LogInformation takes a plain `string message` parameter.
        var source = TestSource.WrapInMethodBody(
            """logger.LogInformation($"Hello, {who:<Recipient>}!");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringLiteralMessage_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("no interpolation here");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTagFormatAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
