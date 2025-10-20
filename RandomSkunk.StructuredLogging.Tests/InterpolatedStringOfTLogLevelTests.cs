using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Tests;

public abstract class InterpolatedStringTests<TLogLevel>
    where TLogLevel : struct, ILogLevel
{
    protected abstract LogLevel EnabledLoggerLogLevel { get; }

    protected abstract LogLevel DisabledLoggerLogLevel { get; }

    [Fact]
    public void GivenLoggerIsEnabledAtSpecifiedLevel_HasFormattedValue()
    {
        Mock<EasyLogger> mockLogger = new();
        mockLogger.Object.MinimumLogLevel = EnabledLoggerLogLevel;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(mockLogger.Object, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().Be("Foo: 123, Bar: True.", "when the logger is enabled, the interpolated string should have a formatted value");
        methodInsideFormatCalled.Should().BeTrue("when the logger is enabled, methods inside of formats should be called");
    }

    [Fact]
    public void GivenLoggerIsNotEnabledAtSpecifiedLevel_HasNullValue()
    {
        Mock<EasyLogger> mockLogger = new();
        mockLogger.Object.MinimumLogLevel = DisabledLoggerLogLevel;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(mockLogger.Object, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().BeNull("when the logger is disabled, the interpolated string shouldn't have a value");
        methodInsideFormatCalled.Should().BeFalse("when the logger is disabled, methods inside of formats should not be called");
    }

    [Fact]
    public void GivenLoggerIsNull_HasNullValue()
    {
        ILogger? logger = null;

        bool methodInsideFormatCalled = false;
        string? value = GetInterpolatedStringValue(logger!, $"Foo: {123}, Bar: {MethodInsideFormat(ref methodInsideFormatCalled)}.");

        value.Should().BeNull("when the logger is null, the interpolated string shouldn't have a value");
        methodInsideFormatCalled.Should().BeFalse("when the logger is disabled, methods inside of formats should not be called");
    }

    private static bool MethodInsideFormat(ref bool called) => called = true;

    private static string? GetInterpolatedStringValue(
        ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        InterpolatedString<TLogLevel> interpolatedString) =>
        interpolatedString.ToStringAndClear();
}

public class InterpolatedStringTraceTests : InterpolatedStringTests<Trace>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Trace;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.Information;
}

public class InterpolatedStringDebugTests : InterpolatedStringTests<Debug>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Debug;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.Information;
}

public class InterpolatedStringInformationTests : InterpolatedStringTests<Information>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Information;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.Warning;
}

public class InterpolatedStringWarningTests : InterpolatedStringTests<Warning>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Warning;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.Error;
}

public class InterpolatedStringErrorTests : InterpolatedStringTests<Error>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Error;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.Critical;
}

public class InterpolatedStringCriticalTests : InterpolatedStringTests<Critical>
{
    protected override LogLevel EnabledLoggerLogLevel => LogLevel.Critical;

    protected override LogLevel DisabledLoggerLogLevel => LogLevel.None;
}
