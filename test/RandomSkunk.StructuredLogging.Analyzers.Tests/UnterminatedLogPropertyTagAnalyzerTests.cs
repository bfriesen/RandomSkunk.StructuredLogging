using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class UnterminatedLogPropertyTagAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Debug($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Information($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Warning($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Error($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Critical($"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Write(LogLevel.Information, $"User {userId:<UserId}");""", "{userId:<UserId}")]
    [InlineData("""logger.Debug($"User {userId:<}");""", "{userId:<}")]
    [InlineData("""logger.Debug($"User {userId:<@UserId}");""", "{userId:<@UserId}")]
    public async Task UnterminatedTag_IsFlagged(string call, string expectedText)
    {
        string source = TestSource.WrapInMethodBody(call);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0008");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);

        SyntaxTree syntaxTree = diagnostic.Location.SourceTree!;
        string text = syntaxTree.GetText().ToString(diagnostic.Location.SourceSpan);
        text.Should().Be(expectedText);
    }

    [Fact]
    public async Task MultipleUnterminatedTags_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"User {userId:<UserId} from {ipAddress:<IpAddress}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task TerminatedTag_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who:<Recipient>}!");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyTagEscapeHatch_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:<>N2}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task PlainInterpolationHole_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonTagFormatStartingWithOtherCharacter_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Value: {who:N2}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UnterminatedTagInPlainStringInterpolation_IsNotFlagged()
    {
        // This interpolated string is converted to `string`, not to one of
        // RandomSkunk.StructuredLogging's interpolated string handler types, because MEL's
        // LogInformation takes a plain `string message` parameter.
        string source = TestSource.WrapInMethodBody(
            """logger.LogInformation($"User {userId:<UserId}");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task StringLiteralMessage_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug("no interpolation here");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UnterminatedLogPropertyTagAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
