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
        var source = TestSource.WrapInMethodBody(call);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0004");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Hidden);
        diagnostic.GetMessage().Should().Contain(expectedPropertyName);
        diagnostic.GetMessage().Should().Contain(expectedValueText);

        var syntaxTree = diagnostic.Location.SourceTree!;
        var text = syntaxTree.GetText().ToString(diagnostic.Location.SourceSpan);
        text.Should().Be($$"""("{{expectedPropertyName}}", {{expectedValueText}})""");
    }

    [Fact]
    public async Task MultipleTupleArguments_AreAllFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello", ("UserId", userId), ("IpAddress", ipAddress));""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("UserId"));
        diagnostics.Select(d => d.GetMessage()).Should().Contain(m => m.Contains("IpAddress"));
    }

    [Fact]
    public async Task ParamsTupleArguments_AreAllFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello", ("UserId", userId), ("IpAddress", ipAddress), ("OrderId", orderId));""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

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
        var source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

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
        var source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoTupleArgument_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TupleArgumentToUnrelatedMethod_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody(
            """
            LocalMethod(("UserId", userId));

            void LocalMethod((string Name, object Value) logProperty1) { }
            """);

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoTupleArguments_LoggerExtensionsCall_IsNotFlagged()
    {
        var source = TestSource.WrapInMethodBody("""logger.LogInformation("no tuple here");""");

        var diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new LogPropertyTupleArgumentAnalyzer());

        diagnostics.Should().BeEmpty();
    }
}
