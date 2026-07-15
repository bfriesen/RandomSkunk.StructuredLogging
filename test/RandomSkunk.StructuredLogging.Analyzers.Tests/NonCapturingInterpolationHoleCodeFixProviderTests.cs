using AwesomeAssertions;

namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

public class NonCapturingInterpolationHoleCodeFixProviderTests
{
    [Theory]
    // Parameter (local): plain name capitalized.
    [InlineData(
        """logger.Debug($"[{ts:HH:mm:ss}]");""",
        """logger.Debug($"[{ts:<Ts>HH:mm:ss}]");""")]
    // No format at all.
    [InlineData(
        """logger.Debug($"Hello, {who}!");""",
        """logger.Debug($"Hello, {who:<Who>}!");""")]
    // Non-tag format, no leading '<'.
    [InlineData(
        """logger.Debug($"Value: {userId:N2}");""",
        """logger.Debug($"Value: {userId:<UserId>N2}");""")]
    // Empty tag opt-out is still a non-capturing hole.
    [InlineData(
        """logger.Debug($"Value: {orderId:<>N2}");""",
        """logger.Debug($"Value: {orderId:<OrderId>N2}");""")]
    // Field with a leading underscore: stripped, then capitalized.
    [InlineData(
        """logger.Debug($"Score: {_userScore}");""",
        """logger.Debug($"Score: {_userScore:<UserScore>}");""")]
    // Property, already capitalized.
    [InlineData(
        """logger.Debug($"Score: {UserScore}");""",
        """logger.Debug($"Score: {UserScore:<UserScore>}");""")]
    // Static "Get"-prefixed method declared in the same type as the call site: guesses just the
    // stripped method name (the declaring type name is omitted since it matches the call site).
    [InlineData(
        """logger.Debug($"Message: {GetMessage()}");""",
        """logger.Debug($"Message: {GetMessage():<Message>}");""")]
    // Not a variable/parameter/field/property/guessable method call: falls back to "PropertyName".
    [InlineData(
        """logger.Debug($"Cast: {(string)userId}");""",
        """logger.Debug($"Cast: {(string)userId:<PropertyName>}");""")]
    // Existing destructuring opt-out tag: '@' is preserved, capture added, trailing format kept.
    [InlineData(
        """logger.Debug($"Value: {value:<@>N2}");""",
        """logger.Debug($"Value: {value:<@Value>N2}");""")]
    public async Task NonCapturingHole_HasTagAdded(string call, string expectedCall)
    {
        var source = TestSource.WrapInMethodBody(call);

        var fixedSource = await CodeFixVerifier.TryApplyFixAsync(
            source, new NonCapturingInterpolationHoleAnalyzer(), new NonCapturingInterpolationHoleCodeFixProvider());

        fixedSource.Should().NotBeNull();
        fixedSource.Should().Be(TestSource.WrapInMethodBody(expectedCall));
    }
}
