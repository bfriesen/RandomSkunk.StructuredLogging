using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Tests;

public class InterpolatedStringTests
{
    [Fact]
    public void GivenLoggerIsEnabledAtSpecifiedLevel_HasFormattedValue()
    {
        Mock<EasyLogger> mockLogger = new();
        mockLogger.Object.MinimumLogLevel = LogLevel.Information;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(mockLogger.Object, LogLevel.Information, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().Be("Foo: 123, Bar: True.", "when the logger is enabled, the interpolated string should have a formatted value");
        methodInsideFormatCalled.Should().BeTrue("when the logger is enabled, methods inside of formats should be called");
    }

    [Fact]
    public void GivenLoggerIsNotEnabledAtSpecifiedLevel_HasNullValue()
    {
        Mock<EasyLogger> mockLogger = new();
        mockLogger.Object.MinimumLogLevel = LogLevel.Warning;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(mockLogger.Object, LogLevel.Information, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().BeNull("when the logger is disabled, the interpolated string shouldn't have a value");
        methodInsideFormatCalled.Should().BeFalse("when the logger is disabled, methods inside of formats should not be called");
    }

    [Fact]
    public void GivenLoggerIsNull_HasNullValue()
    {
        ILogger? logger = null;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(logger!, LogLevel.Critical, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().BeNull("when the logger is null, the interpolated string shouldn't have a value");
        methodInsideFormatCalled.Should().BeFalse("when the logger is disabled, methods inside of formats should not be called");
    }

    private static bool MethodInsideFormat(ref bool called) => called = true;

    private static string? GetInterpolatedStringValue(
        ILogger logger,
        LogLevel level,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(level))]
        InterpolatedString interpolatedString)
    {
        System.Diagnostics.Debug.Assert(logger != null);
        System.Diagnostics.Debug.Assert(Enum.IsDefined(level));

        return interpolatedString.ToStringAndClear();
    }
}
