using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class UndisposedOperationLogCodeFixProviderTests
{
    [Theory]
    [InlineData(
        """logger.BeginOperation("Op");""",
        """using var log = logger.BeginOperation("Op");""")]
    [InlineData(
        """_ = logger.BeginOperation("Op");""",
        """using var log = logger.BeginOperation("Op");""")]
    [InlineData(
        """IOperationLog log = logger.BeginOperation("Op");""",
        """using IOperationLog log = logger.BeginOperation("Op");""")]
    [InlineData(
        """var log = logger.BeginOperation("Op");""",
        """using var log = logger.BeginOperation("Op");""")]
    [InlineData(
        """logger.BeginOperation("Op").AddProperty("Name", "value");""",
        """using var log = logger.BeginOperation("Op").AddProperty("Name", "value");""")]
    public async Task AddUsingDeclaration_RewritesTheFlaggedStatement(string statement, string expectedStatement)
    {
        string source = Wrap(statement);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider(),
            action => action.Title == "Add a 'using' declaration");

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(Wrap(expectedStatement));
    }

    [Fact]
    public async Task AddUsingDeclaration_ForBeginSubOperation_DefaultsToSubLog()
    {
        string source = Wrap(
            """
            using IOperationLog log = logger.BeginOperation("Op");
            log.BeginSubOperation("Sub");
            """);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider(),
            action => action.Title == "Add a 'using' declaration");

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(Wrap(
            """
            using IOperationLog log = logger.BeginOperation("Op");
            using var subLog = log.BeginSubOperation("Sub");
            """));
    }

    [Fact]
    public async Task AddUsingDeclaration_AvoidsCollidingWithAnExistingName()
    {
        string source = Wrap("""logger.BeginOperation("Op");""", extraParameter: "object log");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider(),
            action => action.Title == "Add a 'using' declaration");

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(Wrap("""using var log2 = logger.BeginOperation("Op");""", extraParameter: "object log"));
    }

    [Theory]
    [InlineData(
        """logger.BeginOperation("Op");""",
        """
        using (var log = logger.BeginOperation("Op"))
                {
                }
        """)]
    [InlineData(
        """IOperationLog log = logger.BeginOperation("Op");""",
        """
        using (IOperationLog log = logger.BeginOperation("Op"))
                {
                }
        """)]
    [InlineData(
        """logger.BeginOperation("Op").AddProperty("Name", "value");""",
        """
        using (var log = logger.BeginOperation("Op").AddProperty("Name", "value"))
                {
                }
        """)]
    public async Task AddUsingBlock_WrapsTheFlaggedStatementInAnEmptyBlock(string statement, string expectedStatement)
    {
        string source = Wrap(statement);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider(),
            action => action.Title == "Add a 'using' block");

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(Wrap(expectedStatement));
    }

    [Fact]
    public async Task AddUsingDeclaration_ExistingDeclarationWithFluentChain_KeepsTheChain()
    {
        string source = Wrap("""IOperationLog log = logger.BeginOperation("Op").AddProperty("Name", "value");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider(),
            action => action.Title == "Add a 'using' declaration");

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(Wrap("""using IOperationLog log = logger.BeginOperation("Op").AddProperty("Name", "value");"""));
    }

    [Fact]
    public async Task InvocationNestedInAnArgument_NoFixIsOffered()
    {
        string source = Wrap(
            """Consume(logger.BeginOperation("Op"));""",
            extraMembers: "private static void Consume(IOperationLog log) { }");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new UndisposedOperationLogAnalyzer(), new UndisposedOperationLogCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    private static string Wrap(string statements, string extraMembers = "", string extraParameter = "") => $$"""
        using System;
        using Microsoft.Extensions.Logging;
        using RandomSkunk.StructuredLogging.Operation;

        namespace TestNamespace;

        public class TestClass
        {
            {{extraMembers}}

            public void TestMethod(ILogger logger{{(extraParameter.Length == 0 ? "" : $", {extraParameter}")}})
            {
                {{statements}}
            }
        }
        """;
}
