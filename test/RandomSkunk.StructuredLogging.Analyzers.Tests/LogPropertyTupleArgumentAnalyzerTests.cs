using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class LogPropertyTupleArgumentAnalyzerTests
{
    [Theory]
    [InlineData("""logger.Trace($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Debug($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Information($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Warning($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Error($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Critical($"Hello", ("UserName", userName));""", "UserName", "userName")]
    [InlineData("""logger.Write(LogLevel.Information, $"Hello", ("UserName", userName));""", "UserName", "userName")]
    public async Task TupleArgument_IsFlagged(string call, string expectedPropertyName, string expectedValueText)
    {
        string source = TestSource.WrapInMethodBody(call);

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0004");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Hidden);
        diagnostic.GetMessage().Should().Contain(expectedPropertyName);
        diagnostic.GetMessage().Should().Contain(expectedValueText);

        SyntaxTree syntaxTree = diagnostic.Location.SourceTree!;
        string text = syntaxTree.GetText().ToString(diagnostic.Location.SourceSpan);
        text.Should().Be($$"""("{{expectedPropertyName}}", {{expectedValueText}})""");
    }

    [Fact]
    public async Task MultipleTupleArguments_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello", ("UserId", userId), ("IpAddress", ipAddress));""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("UserId"));
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("IpAddress"));
    }

    [Fact]
    public async Task ParamsTupleArguments_AreAllFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello", ("UserId", userId), ("IpAddress", ipAddress), ("OrderId", orderId));""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("""("User" + "Id", userId)""", "UserId")]
    [InlineData("""($"UserId", userId)""", "UserId")]
    [InlineData("""(ConstantPart, userId)""", "Const")]
    [InlineData("""(ConstantPart + "Id", userId)""", "ConstId")]
    [InlineData("""($"{ConstantPart}Id", userId)""", "ConstId")]
    [InlineData("""($"{ConstantPart}" + "Id", userId)""", "ConstId")]
    [InlineData("""(("User" + "Id"), userId)""", "UserId")]
    public async Task ConstantNonLiteralPropertyName_IsFlaggedWithItsFoldedValue(string tupleArgument, string expectedPropertyName)
    {
        string source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain(expectedPropertyName);
    }

    [Theory]
    [InlineData("""(dynamicName, userId)""")]
    [InlineData("""(GetMessage(), userId)""")]
    [InlineData("""(dynamicName + "Id", userId)""")]
    [InlineData("""($"{dynamicName}", userId)""")]
    [InlineData("""($"Id{dynamicName}", userId)""")]
    public async Task NonConstantPropertyName_IsNotFlagged(string tupleArgument)
    {
        string source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoTupleArgument_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TupleArgumentToUnrelatedMethod_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody(
            """
            LocalMethod(("UserId", userId));

            void LocalMethod((string Name, object Value) logProperty1) { }
            """);

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoTupleArguments_LoggerExtensionsCall_IsNotFlagged()
    {
        string source = TestSource.WrapInMethodBody("""logger.LogInformation("no tuple here");""");

        System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
