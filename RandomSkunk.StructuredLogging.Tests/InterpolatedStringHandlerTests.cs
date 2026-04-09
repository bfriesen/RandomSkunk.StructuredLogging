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
        InterpolatedString.Message handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.Message handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.Message handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.Message handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWriteMessage(LogLevel.Information, $"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.TraceMessage handler = logger.GetTraceMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetTraceMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetTraceMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.TraceMessage handler = logger.GetTraceMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetTraceMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.TraceMessage handler = logger.GetTraceMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetTraceMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetTraceMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetTraceMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.TraceMessage handler = logger.GetTraceMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetTraceMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.DebugMessage handler = logger.GetDebugMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetDebugMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetDebugMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.DebugMessage handler = logger.GetDebugMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetDebugMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.DebugMessage handler = logger.GetDebugMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetDebugMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetDebugMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetDebugMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.DebugMessage handler = logger.GetDebugMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetDebugMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.InformationMessage handler = logger.GetInformationMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetInformationMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetInformationMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.InformationMessage handler = logger.GetInformationMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetInformationMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.InformationMessage handler = logger.GetInformationMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetInformationMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetInformationMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetInformationMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.InformationMessage handler = logger.GetInformationMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetInformationMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.WarningMessage handler = logger.GetWarningMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetWarningMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetWarningMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.WarningMessage handler = logger.GetWarningMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWarningMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.WarningMessage handler = logger.GetWarningMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetWarningMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetWarningMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetWarningMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.WarningMessage handler = logger.GetWarningMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetWarningMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.ErrorMessage handler = logger.GetErrorMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetErrorMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetErrorMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.ErrorMessage handler = logger.GetErrorMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetErrorMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.ErrorMessage handler = logger.GetErrorMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetErrorMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetErrorMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetErrorMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.ErrorMessage handler = logger.GetErrorMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetErrorMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
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

        InterpolatedString.CriticalMessage handler = logger.GetCriticalMessage($"Foo: '{foo}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,10:B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified and both format and alignment are specified");

        handler = logger.GetCriticalMessage($"Bar: '{bar}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Bar: '{bar,-5}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified and alignment is specified");

        handler = logger.GetCriticalMessage($"Baz: '{baz}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the format does not specify a log property name");
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

        InterpolatedString.CriticalMessage handler = logger.GetCriticalMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("foo", foo));
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetCriticalMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("bar", bar.ToString()));
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
            .Which.Should().BeEquivalentTo(new KeyValuePair<string, object?>("baz", baz));
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().ContainSingle("the format specifies a log property name")
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

        InterpolatedString.CriticalMessage handler = logger.GetCriticalMessage($"Foo: '{foo:<>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '1111011'.", "the logger is enabled and format is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '123  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Foo: '{foo,10:<>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Foo: '   1111011'.", "the logger is enabled and both format and alignment are specified");

        handler = logger.GetCriticalMessage($"Bar: '{bar:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Bar: '{bar,-5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Bar: 'abc  '.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Baz: '{baz:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: 'xyz'.", "the logger is enabled");

        handler = logger.GetCriticalMessage($"Baz: '{baz,5:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
        messageData.Message.Should().Be("Baz: '  xyz'.", "the logger is enabled and alignment is specified");

        handler = logger.GetCriticalMessage($"Qux: '{qux,-20:<>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is enabled but the log property key is empty");
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

        InterpolatedString.CriticalMessage handler = logger.GetCriticalMessage($"Foo: '{foo:<foo>}'.");
        MessageData messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo,-5:<foo>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Foo: '{foo,10:<foo>B}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Bar: '{bar:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Bar: '{bar,-5:<bar>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Baz: '{baz:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Baz: '{baz,5:<baz>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");

        handler = logger.GetCriticalMessage($"Qux: '{qux,-20:<qux>}'.");
        messageData = handler.GetMessageDataAndClear();
        messageData.AdditionalNameValuePairs.Should().BeEmpty("the logger is disabled");
        messageData.Message.Should().BeNull("the logger is disabled");
    }
}

[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters are required by interpolated string handler argument")]
internal static class InterpolatedStringHandlerExtensions
{
    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.Message GetWriteMessage(
        this ILogger logger,
        LogLevel level,
        [InterpolatedStringHandlerArgument(nameof(logger), nameof(level))]
        ref InterpolatedString.Message handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.TraceMessage GetTraceMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.TraceMessage handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.DebugMessage GetDebugMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.DebugMessage handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.InformationMessage GetInformationMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.InformationMessage handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.WarningMessage GetWarningMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.WarningMessage handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.ErrorMessage GetErrorMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.ErrorMessage handler) => handler;

    /// <summary>Extension method for testing.</summary>
    public static InterpolatedString.CriticalMessage GetCriticalMessage(
        this ILogger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))]
        ref InterpolatedString.CriticalMessage handler) => handler;
}
