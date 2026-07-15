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
        var source = TestSource.WrapInMethodBody(call);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }

    [Fact]
    public async Task PlainStringLiteralMessage_IsJustRenamed()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("Hello, there!");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, there!");"""));
    }

    [Fact]
    public async Task TagFormatHole_BecomesInPlacePlaceholder()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {userName:<UserName>}!");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, {UserName}!", userName);"""));
    }

    [Fact]
    public async Task TagFormatHoleWithResidualFormat_BecomesInPlacePlaceholderWithFormat()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"[{ts:<Timestamp>HH:mm:ss}] Started");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("[{Timestamp:HH:mm:ss}] Started", ts);"""));
    }

    [Fact]
    public async Task DestructuringTag_BecomesAtPlaceholder()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {value:<@Order>}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Order: {@Order}", value);"""));
    }

    [Fact]
    public async Task BareDestructuringTag_GuessesNameFromExpression()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {value:<@>}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Order: {@Value}", value);"""));
    }

    [Fact]
    public async Task BareDestructuringTagOnUnguessableExpression_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Order: {(string)userId:<@>}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHole_GuessesNameFromExpression()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {who}!");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hello, {Who}!", who);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnUnguessableExpression_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello, {(string)userId}!");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnInstanceFieldViaThis_GuessesJustTheFieldName()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Score: {this._userScore}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Score: {UserScore}", this._userScore);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnBareInstanceField_GuessesJustTheFieldName()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Score: {_userScore}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Score: {UserScore}", _userScore);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticProperty_GuessesTypeNamePlusMemberName()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"NL: {Environment.NewLine}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("NL: {EnvironmentNewLine}", Environment.NewLine);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaLocal_GuessesLocalNamePlusMemberName()
    {
        var source = TestSource.WrapInMethodBody(
            """
            var user = new { Name = who };
            logger.Debug($"Hello, {user.Name}!");
            """);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
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
        var source = TestSource.WrapInMethodBody(
            """
            var order = new { Customer = new { Name = who } };
            logger.Debug($"Hello, {order.Customer.Name}!");
            """);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
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
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Message: {GetMessage()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Message: {Message}", GetMessage());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticCreateMethodInSameType_GuessesStrippedNameOnly()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Default: {CreateDefault()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Default: {Default}", CreateDefault());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnStaticGetMethodInDifferentType_GuessesTypeNamePlusStrippedName()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Zones: {System.TimeZoneInfo.GetSystemTimeZones()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Zones: {TimeZoneInfoSystemTimeZones}", System.TimeZoneInfo.GetSystemTimeZones());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnBareInstanceGetMethod_GuessesStrippedNameOnly()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Type: {GetType()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Type: {Type}", GetType());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnInstanceGetMethodViaLocal_GuessesTargetPlusStrippedName()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Type: {who.GetType()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Type: {WhoType}", who.GetType());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnNestedMethodChain_GuessesEverySegment()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hash: {who.GetType().GetHashCode()}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Hash: {WhoTypeHashCode}", who.GetType().GetHashCode());"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaMethodCallTarget_GuessesEverySegment()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Length: {GetMessage().Length}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("Length: {MessageLength}", GetMessage().Length);"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnPropertyViaUnguessableMethodCallTarget_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Length: {GetMessage().ToUpper().Length}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerialize_GuessesFromFirstArgument()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"User: {System.Text.Json.JsonSerializer.Serialize(userId)}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("User: {UserId}", System.Text.Json.JsonSerializer.Serialize(userId));"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerializeOfMemberAccess_GuessesFromFirstArgument()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Score: {System.Text.Json.JsonSerializer.Serialize(this._userScore)}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Score: {UserScore}", System.Text.Json.JsonSerializer.Serialize(this._userScore));"""));
    }

    [Fact]
    public async Task NonCapturingHoleOnSystemTextJsonSerializeOfUnguessableArgument_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Value: {System.Text.Json.JsonSerializer.Serialize(GetMessage().ToUpper())}");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonCapturingHoleOnNewtonsoftSerializeObject_GuessesFromFirstArgument()
    {
        var source = """
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

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Contain("""logger.LogDebug("User: {User}", Newtonsoft.Json.JsonConvert.SerializeObject(user));""");
    }

    [Fact]
    public async Task EmptyOptOutTag_GuessesNameAndKeepsRemainingFormat()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"[{ts:<>HH:mm:ss}] Started");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("[{Ts:HH:mm:ss}] Started", ts);"""));
    }

    [Fact]
    public async Task ExplicitTupleProperty_IsAppendedWithLeadingSpace()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"User signed in", ("UserId", userId));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("User signed in {UserId}", userId);"""));
    }

    [Fact]
    public async Task MultipleExplicitTupleProperties_AreAllAppendedInOrder()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Request handled", ("UserId", userId), ("IpAddress", ipAddress));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Request handled {UserId} {IpAddress}", userId, ipAddress);"""));
    }

    [Fact]
    public async Task ParamsTupleProperties_AreAllAppendedInOrder()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Request handled", ("UserId", userId), ("IpAddress", ipAddress), ("OrderId", orderId));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(
            """logger.LogDebug("Request handled {UserId} {IpAddress} {OrderId}", userId, ipAddress, orderId);"""));
    }

    [Fact]
    public async Task ExplicitTuplePropertyOnPlainStringLiteralMessage_IsAppended()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug("User signed in", ("UserId", userId));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody("""logger.LogDebug("User signed in {UserId}", userId);"""));
    }

    [Fact]
    public async Task ExplicitPropertyCombinedWithTagFormatHole_BothAreCaptured()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug($"Hello, {userName:<UserName>}!", ("IpAddress", ipAddress));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
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
        var source = TestSource.WrapInMethodBody($$"""logger.Debug($"Hello", {{tupleArgument}});""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody($$"""logger.LogDebug("Hello {{{expectedPropertyName}}}", userId);"""));
    }

    [Fact]
    public async Task NonConstantTuplePropertyName_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody("""logger.Debug($"Hello", (dynamicName, userId));""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task LeadingCollectionArgument_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody(
            """logger.Debug(new System.Collections.Generic.Dictionary<string, object?> { ["UserId"] = userId }, $"Hello");""");

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }

    [Fact]
    public async Task NonLiteralMessage_IsNotFixed()
    {
        var source = TestSource.WrapInMethodBody(
            """
            var message = "Hello";
            logger.Debug(message);
            """);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new StructuredLoggerExtensionsInvocationAnalyzer(), new StructuredLoggerExtensionsInvocationCodeFixProvider());

        fixedSource.Should().BeNull();
    }
}
