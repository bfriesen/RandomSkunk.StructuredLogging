using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class StructuredLoggerExtensionsInvocationCodeFixProviderTests
{
    [Theory]
    [InlineData("""logger.Trace($"Hello, there!");""", """logger.LogTrace("Hello, there!");""")]
    [InlineData("""logger.Debug($"Hello, there!");""", """logger.LogDebug("Hello, there!");""")]
    [InlineData("""logger.Information($"Hello, there!");""", """logger.LogInformation("Hello, there!");""")]
    [InlineData("""logger.Warning($"Hello, there!");""", """logger.LogWarning("Hello, there!");""")]
    [InlineData("""logger.Error($"Hello, there!");""", """logger.LogError("Hello, there!");""")]
    [InlineData("""logger.Critical($"Hello, there!");""", """logger.LogCritical("Hello, there!");""")]
    [InlineData("""logger.Write(LogLevel.Information, $"Hello, there!");""", """logger.Log(LogLevel.Information, "Hello, there!");""")]
    [InlineData(
        """logger.Debug(new EventId(1, "Name"), $"Hello, there!");""",
        """logger.LogDebug(new EventId(1, "Name"), "Hello, there!");""")]
    [InlineData(
        """logger.Error(new Exception(), $"Hello, there!");""",
        """logger.LogError(new Exception(), "Hello, there!");""")]
    [InlineData(
        """logger.Critical(new EventId(1, "Name"), new Exception(), $"Hello, there!");""",
        """logger.LogCritical(new EventId(1, "Name"), new Exception(), "Hello, there!");""")]
    public async Task PlainMessage_IsJustRenamed(string call, string expectedCall)
    {
        string source = TestSource.WrapInMethodBody(call);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }

    [Fact]
    public async Task PlainStringLiteralMessage_IsJustRenamed()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug("Hello, there!");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, there!");"""));
    }

    [Fact]
    public async Task TagFormatHole_BecomesInPlacePlaceholder()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {userName:<UserName>}!");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, {UserName}!", userName);"""));
    }

    [Fact]
    public async Task TagFormatHoleWithResidualFormat_BecomesInPlacePlaceholderWithFormat()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"[{ts:<Timestamp>HH:mm:ss}] Started");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("[{Timestamp:HH:mm:ss}] Started", ts);"""));
    }

    [Fact]
    public async Task DestructuringTag_BecomesAtPlaceholder()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {value:<@Order>}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Order: {@Order}", value);"""));
    }

    [Fact]
    public async Task BareDestructuringTag_GuessesNameFromExpression()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {value:<@>}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Order: {@Value}", value);"""));
    }

    [Fact]
    public async Task BareDestructuringTagOnUnguessableExpression_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {(string)userId:<@>}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHole_GuessesNameFromExpression()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, {Who}!", who);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnUnguessableExpression_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {(string)userId}!");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnInstanceFieldViaThis_GuessesJustTheFieldName()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Score: {this._userScore}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Score: {UserScore}", this._userScore);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnBareInstanceField_GuessesJustTheFieldName()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Score: {_userScore}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Score: {UserScore}", _userScore);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticProperty_GuessesTypeNamePlusMemberName()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"NL: {Environment.NewLine}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("NL: {EnvironmentNewLine}", Environment.NewLine);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaLocal_GuessesLocalNamePlusMemberName()
    {
        string source = TestSource.WrapInMethodBody(
            """
            var user = new { Name = who };
            logger.Debug($"Hello, {user.Name}!");
            """);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """
            var user = new { Name = who };
            logger.LogDebug("Hello, {UserName}!", user.Name);
            """));
    }

    [Fact]
    public async Task NonCapturingHoleOnNestedPropertyChain_GuessesEverySegment()
    {
        string source = TestSource.WrapInMethodBody(
            """
            var order = new { Customer = new { Name = who } };
            logger.Debug($"Hello, {order.Customer.Name}!");
            """);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """
            var order = new { Customer = new { Name = who } };
            logger.LogDebug("Hello, {OrderCustomerName}!", order.Customer.Name);
            """));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticGetMethodInSameType_GuessesStrippedNameOnly()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Message: {GetMessage()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Message: {Message}", GetMessage());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticCreateMethodInSameType_GuessesStrippedNameOnly()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Default: {CreateDefault()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Default: {Default}", CreateDefault());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticGetMethodInDifferentType_GuessesTypeNamePlusStrippedName()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Zones: {System.TimeZoneInfo.GetSystemTimeZones()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Zones: {TimeZoneInfoSystemTimeZones}", System.TimeZoneInfo.GetSystemTimeZones());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnBareInstanceGetMethod_GuessesStrippedNameOnly()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Type: {GetType()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Type: {Type}", GetType());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnInstanceGetMethodViaLocal_GuessesTargetPlusStrippedName()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Type: {who.GetType()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Type: {WhoType}", who.GetType());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnNestedMethodChain_GuessesEverySegment()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hash: {who.GetType().GetHashCode()}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hash: {WhoTypeHashCode}", who.GetType().GetHashCode());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaMethodCallTarget_GuessesEverySegment()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Length: {GetMessage().Length}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Length: {MessageLength}", GetMessage().Length);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaUnguessableMethodCallTarget_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Length: {GetMessage().ToUpper().Length}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerialize_GuessesFromFirstArgument()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"User: {System.Text.Json.JsonSerializer.Serialize(userId)}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("User: {UserId}", System.Text.Json.JsonSerializer.Serialize(userId));"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerializeOfMemberAccess_GuessesFromFirstArgument()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Score: {System.Text.Json.JsonSerializer.Serialize(this._userScore)}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Score: {UserScore}", System.Text.Json.JsonSerializer.Serialize(this._userScore));"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerializeOfUnguessableArgument_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Value: {System.Text.Json.JsonSerializer.Serialize(GetMessage().ToUpper())}");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnNewtonsoftSerializeObject_GuessesFromFirstArgument()
    {
        string source = """
            using Microsoft.Extensions.Logging;
            using RandomSkunk.StructuredLogging;

            namespace Newtonsoft.Json
            {
                public static class JsonConvert
                {
                    public static string SerializeObject(object? value) => value?.ToString() ?? "";
                }
            }

            namespace TestNamespace
            {
                public class TestClass
                {
                    public void TestMethod(ILogger logger, object user)
                    {
                        logger.Debug($"User: {Newtonsoft.Json.JsonConvert.SerializeObject(user)}");
                    }
                }
            }
            """;

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Contain("""logger.LogDebug("User: {User}", Newtonsoft.Json.JsonConvert.SerializeObject(user));""");
    }

    [Fact]
    public async Task EmptyOptOutTag_GuessesNameAndKeepsRemainingFormat()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"[{ts:<>HH:mm:ss}] Started");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("[{Ts:HH:mm:ss}] Started", ts);"""));
    }

    [Fact]
    public async Task ExplicitTupleProperty_IsAppendedWithLeadingSpace()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"User signed in", ("UserId", userId));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("User signed in {UserId}", userId);"""));
    }

    [Fact]
    public async Task MultipleExplicitTupleProperties_AreAllAppendedInOrder()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Request handled", ("UserId", userId), ("IpAddress", ipAddress));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Request handled {UserId} {IpAddress}", userId, ipAddress);"""));
    }

    [Fact]
    public async Task ParamsTupleProperties_AreAllAppendedInOrder()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Request handled", ("UserId", userId), ("IpAddress", ipAddress), ("OrderId", orderId));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Request handled {UserId} {IpAddress} {OrderId}", userId, ipAddress, orderId);"""));
    }

    [Fact]
    public async Task ExplicitTuplePropertyOnPlainStringLiteralMessage_IsAppended()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug("User signed in", ("UserId", userId));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("User signed in {UserId}", userId);"""));
    }

    [Fact]
    public async Task ExplicitPropertyCombinedWithTagFormatHole_BothAreCaptured()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello, {userName:<UserName>}!", ("IpAddress", ipAddress));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Hello, {UserName}! {IpAddress}", userName, ipAddress);"""));
    }

    [Theory]
    [InlineData("""("User" + "Id", userId)""", "UserId")]
    [InlineData("""(ConstantPart, userId)""", "Const")]
    public async Task ConstantNonLiteralPropertyName_UsesItsFoldedValue(string tupleArgument, string expectedPropertyName)
    {
        string source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody($$"""logger.LogDebug("Hello {{{expectedPropertyName}}}", userId);"""));
    }

    [Fact]
    public async Task NonConstantTuplePropertyName_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody("""logger.Debug($"Hello", (dynamicName, userId));""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task LeadingCollectionArgument_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody(
            """logger.Debug(new System.Collections.Generic.Dictionary<string, object?> { ["UserId"] = userId }, $"Hello");""");

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonLiteralMessage_IsNotFixed()
    {
        string source = TestSource.WrapInMethodBody(
            """
            var message = "Hello";
            logger.Debug(message);
            """);

        string? fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }
}
