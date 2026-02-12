using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RandomSkunk.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Tests;

public class WriteInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;
        logger.Information($"{123}");
        WriteInterpolatedStringHandler handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;

        WriteInterpolatedStringHandler handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;

        WriteInterpolatedStringHandler handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        WriteInterpolatedStringHandler handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteInterpolatedStringHandler(LogLevel.Information, $"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class TraceInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Trace;

        TraceInterpolatedStringHandler handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Trace;

        TraceInterpolatedStringHandler handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Trace;

        TraceInterpolatedStringHandler handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        TraceInterpolatedStringHandler handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class DebugInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Debug;

        DebugInterpolatedStringHandler handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Debug;

        DebugInterpolatedStringHandler handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Debug;

        DebugInterpolatedStringHandler handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        DebugInterpolatedStringHandler handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class InformationInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;

        InformationInterpolatedStringHandler handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;

        InformationInterpolatedStringHandler handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Information;

        InformationInterpolatedStringHandler handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        InformationInterpolatedStringHandler handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class WarningInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Warning;

        WarningInterpolatedStringHandler handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Warning;

        WarningInterpolatedStringHandler handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Warning;

        WarningInterpolatedStringHandler handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        WarningInterpolatedStringHandler handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class ErrorInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Error;

        ErrorInterpolatedStringHandler handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Error;

        ErrorInterpolatedStringHandler handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Error;

        ErrorInterpolatedStringHandler handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        ErrorInterpolatedStringHandler handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

public class CriticalInterpolatedStringHandlerTests
{
    [Fact]
    public void GivenLoggerIsEnabled_HasFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Critical;

        CriticalInterpolatedStringHandler handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsSpecified_HasFormattedValueAndLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Critical;

        CriticalInterpolatedStringHandler handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("qux", qux));
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsEnabledAndLogPropertyKeyIsEmpty_HasFormattedValueButNotLogProperty()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.Critical;

        CriticalInterpolatedStringHandler handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalInterpolatedStringHandler($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Qux: 'http://qux.com/     '.", "the logger is enabled and alignment is specified");
    }

    [Fact]
    public void GivenLoggerIsDisabled_DoesNotHaveFormattedValue()
    {
        const int foo = 123;
        ReadOnlySpan<char> bar = "abc".AsSpan();
        const string baz = "xyz";
        object qux = new Uri("http://qux.com/");

        EasyLogger logger = new Mock<EasyLogger>().Object;
        logger.MinimumLogLevel = LogLevel.None;

        CriticalInterpolatedStringHandler handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalInterpolatedStringHandler($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.InterpolationNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters are required by interpolated string handler argument")]
internal static class InterpolatedStringHandlerExtensions
{
    public static WriteInterpolatedStringHandler GetWriteInterpolatedStringHandler(
        this ILogger logger,
        LogLevel level,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(level))]
        ref WriteInterpolatedStringHandler handler) => handler;

    public static TraceInterpolatedStringHandler GetTraceInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref TraceInterpolatedStringHandler handler) => handler;

    public static DebugInterpolatedStringHandler GetDebugInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref DebugInterpolatedStringHandler handler) => handler;

    public static InformationInterpolatedStringHandler GetInformationInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InformationInterpolatedStringHandler handler) => handler;

    public static WarningInterpolatedStringHandler GetWarningInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref WarningInterpolatedStringHandler handler) => handler;

    public static ErrorInterpolatedStringHandler GetErrorInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref ErrorInterpolatedStringHandler handler) => handler;

    public static CriticalInterpolatedStringHandler GetCriticalInterpolatedStringHandler(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref CriticalInterpolatedStringHandler handler) => handler;
}
