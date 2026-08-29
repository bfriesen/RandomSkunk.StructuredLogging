using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class UndisposedOperationLogAnalyzerTests
{
    [Theory]
    [InlineData("""logger.BeginOperation("Op");""", "BeginOperation")]
    [InlineData("""logger.BeginOperation(new EventId(1, "Name"), "Op");""", "BeginOperation")]
    [InlineData("""_ = logger.BeginOperation("Op");""", "BeginOperation")]
    [InlineData("""IOperationLog log = logger.BeginOperation("Op");""", "BeginOperation")]
    public async Task UndisposedBeginOperation_IsFlagged(string call, string methodName)
    {
        string source = Wrap(call);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().ContainSingle();
        Diagnostic diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("RSSL0006");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain(methodName);
    }

    [Fact]
    public async Task UndisposedBeginSubOperation_IsFlagged()
    {
        string source = Wrap(
            """
            using IOperationLog log = logger.BeginOperation("Op");
            ISubOperationLog subLog = log.BeginSubOperation("SubOp");
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("BeginSubOperation");
    }

    [Theory]
    [InlineData("""using IOperationLog log = logger.BeginOperation("Op");""")]
    [InlineData("""using (IOperationLog log = logger.BeginOperation("Op")) { }""")]
    [InlineData("""using var log = logger.BeginOperation("Op");""")]
    [InlineData(
        """
        IOperationLog log = logger.BeginOperation("Op");
        log.Dispose();
        """)]
    [InlineData(
        """
        IOperationLog log = logger.BeginOperation("Op");
        using (log) { }
        """)]
    [InlineData(
        """
        IOperationLog log = logger.BeginOperation("Op");
        ((IDisposable)log).Dispose();
        """)]
    [InlineData("""using var log = logger.BeginOperation("Op").AddProperty("Name", "value");""")]
    [InlineData("""using var log = logger.BeginOperation("Op").SetResult(1).AddProperty("Name", "value");""")]
    [InlineData(
        """
        IOperationLog log = logger.BeginOperation("Op");
        log.AddProperty("Name", "value").Dispose();
        """)]
    public async Task DisposedBeginOperation_IsNotFlagged(string statements)
    {
        string source = Wrap(statements);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DisposedBeginSubOperation_IsNotFlagged()
    {
        string source = Wrap(
            """
            using IOperationLog log = logger.BeginOperation("Op");
            using ISubOperationLog subLog = log.BeginSubOperation("SubOp");
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UndisposedBeginOperation_ThroughFluentChain_IsFlagged()
    {
        // The `AddProperty` call makes this a false positive risk: the diagnostic anchors on the
        // inner `BeginOperation` call, but the value that actually needs disposing is the result
        // of the whole chain.
        string source = Wrap("""logger.BeginOperation("Op").AddProperty("Name", "value");""");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().ContainSingle();
    }

    [Fact]
    public async Task DisposedBeginSubOperation_ThroughFluentChain_IsNotFlagged()
    {
        string source = Wrap(
            """
            using IOperationLog log = logger.BeginOperation("Op");
            using ISubOperationLog subLog = log.BeginSubOperation("SubOp").AddProperty("Name", "value");
            """);

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnedOperationLog_IsNotFlagged()
    {
        string source = $$"""
            using System;
            using Microsoft.Extensions.Logging;
            using RandomSkunk.StructuredLogging.Operation;

            namespace TestNamespace;

            public class TestClass
            {
                public IOperationLog TestMethod(ILogger logger)
                {
                    return logger.BeginOperation("Op");
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task OperationLogPassedAsArgument_IsFlagged()
    {
        // Passing an IOperationLog as a plain argument doesn't transfer disposal
        // responsibility - developers are expected to create and dispose sub-operations within
        // the method they're threaded into, not hand off ownership through a parameter.
        string source = Wrap(
            """
            IOperationLog log = logger.BeginOperation("Op");
            Consume(log);
            """,
            extraMembers: "private static void Consume(IOperationLog log) { }");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().ContainSingle();
    }

    [Fact]
    public async Task OperationLogAssignedToField_IsNotFlagged()
    {
        string source = Wrap(
            """_field = logger.BeginOperation("Op");""",
            extraMembers: "private IOperationLog _field = null!;");

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerVerifier.GetDiagnosticsAsync(source, new UndisposedOperationLogAnalyzer());

        diagnostics.Should().BeEmpty();
    }

    private static string Wrap(string statements, string extraMembers = "") => $$"""
        using System;
        using Microsoft.Extensions.Logging;
        using RandomSkunk.StructuredLogging.Operation;

        namespace TestNamespace;

        public class TestClass
        {
            {{extraMembers}}

            public void TestMethod(ILogger logger)
            {
                {{statements}}
            }
        }
        """;
}
