using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Tests;

public class GeneratedLoggerExtensionsTests
{
    [Flags]
    private enum MethodDefinition
    {
        // A: Method Name (3 bit integer)
        // B: Whether event id parameter is present
        // C: Whether exception parameter is present
        // D: Whether message parameter is string (true) or interpolated string (false)
        // E: Data type (4 bit integer)

        //                 EEEE  DCB  AAA
        None =          0b_0000_0000_0000,
        Write =         0b_0000_0000_0001,
        Trace =         0b_0000_0000_0010,
        Debug =         0b_0000_0000_0011,
        Information =   0b_0000_0000_0100,
        Warning =       0b_0000_0000_0101,
        Error =         0b_0000_0000_0110,
        Critical =      0b_0000_0000_0111,
        NameMask =      0b_0000_0000_1111,
        EventId =       0b_0000_0001_0000,
        Exception =     0b_0000_0010_0000,
        StringMessage = 0b_0000_0100_0000,
        TupleArray =    0b_0001_0000_0000,
        OneTuple =      0b_0010_0000_0000,
        TwoTuple =      0b_0011_0000_0000,
        ThreeTuple =    0b_0100_0000_0000,
        FourTuple =     0b_0101_0000_0000,
        FiveTuple =     0b_0110_0000_0000,
        SixTuple =      0b_0111_0000_0000,
        SevenTuple =    0b_1000_0000_0000,
        EightTuple =    0b_1001_0000_0000,
        KvpCollection = 0b_1010_0000_0000,
        KvpList =       0b_1011_0000_0000,
        DataTypeMask =  0b_1111_0000_0000,
    }

    [Fact]
    public void MethodsWithTheCorrectSignaturesAreGenerated()
    {
        MethodDefinition[] methodDefinitions = [..
            typeof(StructuredLoggingExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method =>
                    method.GetParameters() is { Length: > 0 } parameters
                    && parameters[0].ParameterType == typeof(ILogger)
                    && method.ReturnType == typeof(void)
                    && method.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(ExtensionAttribute)))
                .Select(method =>
                {
                    Type[] parameterTypes = [.. method.GetParameters().Select(p => p.ParameterType)];
                    Type[] genericArguments = method.GetGenericArguments();

                    MethodDefinition methodDefinition = MethodDefinition.None;
                    int floatingParameterIndex = 1;

                    methodDefinition |= method.Name switch
                    {
                        nameof(StructuredLoggingExtensions.Write) => MethodDefinition.Write,
                        nameof(StructuredLoggingExtensions.Trace) => MethodDefinition.Trace,
                        nameof(StructuredLoggingExtensions.Debug) => MethodDefinition.Debug,
                        nameof(StructuredLoggingExtensions.Information) => MethodDefinition.Information,
                        nameof(StructuredLoggingExtensions.Warning) => MethodDefinition.Warning,
                        nameof(StructuredLoggingExtensions.Error) => MethodDefinition.Error,
                        nameof(StructuredLoggingExtensions.Critical) => MethodDefinition.Critical,
                        _ => throw new Exception("Unexpected extension method name."),
                    };

                    if ((methodDefinition & MethodDefinition.NameMask) == MethodDefinition.Write)
                    {
                        parameterTypes.Should().HaveElementAt(1, typeof(LogLevel));
                        floatingParameterIndex = 2;
                    }

                    int interpolatedMessageParameterIndex = -1;

                    if (parameterTypes[floatingParameterIndex] == typeof(EventId))
                    {
                        methodDefinition |= MethodDefinition.EventId;

                        if (parameterTypes[floatingParameterIndex + 1] == typeof(Exception))
                        {
                            methodDefinition |= MethodDefinition.Exception;

                            if (parameterTypes[floatingParameterIndex + 2] ==  typeof(string))
                            {
                                methodDefinition |= MethodDefinition.StringMessage;
                            }
                            else
                            {
                                interpolatedMessageParameterIndex = floatingParameterIndex + 2;
                            }
                        }
                        else if (parameterTypes[floatingParameterIndex + 1] == typeof(string))
                        {
                            methodDefinition |= MethodDefinition.StringMessage;
                        }
                        else
                        {
                            interpolatedMessageParameterIndex = floatingParameterIndex + 1;
                        }
                    }
                    else if (parameterTypes[floatingParameterIndex] == typeof(Exception))
                    {
                        methodDefinition |= MethodDefinition.Exception;

                        if (parameterTypes[floatingParameterIndex + 1] == typeof(string))
                        {
                            methodDefinition |= MethodDefinition.StringMessage;
                        }
                        else
                        {
                            interpolatedMessageParameterIndex = floatingParameterIndex + 1;
                        }
                    }
                    else if (parameterTypes[floatingParameterIndex] == typeof(string))
                    {
                        methodDefinition |= MethodDefinition.StringMessage;
                    }
                    else
                    {
                        interpolatedMessageParameterIndex = floatingParameterIndex;
                    }

                    if (interpolatedMessageParameterIndex > -1)
                    {
                        parameterTypes[interpolatedMessageParameterIndex].Should().Be(
                            method.Name switch
                            {
                                nameof(StructuredLoggingExtensions.Write) => typeof(InterpolatedString.Message).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Trace) => typeof(InterpolatedString.TraceMessage).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Debug) => typeof(InterpolatedString.DebugMessage).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Information) => typeof(InterpolatedString.InformationMessage).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Warning) => typeof(InterpolatedString.WarningMessage).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Error) => typeof(InterpolatedString.ErrorMessage).MakeByRefType(),
                                nameof(StructuredLoggingExtensions.Critical) => typeof(InterpolatedString.CriticalMessage).MakeByRefType(),
                                _ => throw new Exception("Unexpected extension method name."),
                            });
                    }

                    methodDefinition |= genericArguments.Length switch
                    {
                        0 when parameterTypes[^1] == typeof((string, object)[]) => MethodDefinition.TupleArray,
                        0 when parameterTypes[^1] == typeof(IReadOnlyCollection<KeyValuePair<string, object?>>) => MethodDefinition.KvpCollection,
                        1 when parameterTypes[^1].IsGenericParameter => MethodDefinition.KvpList,
                        1 => MethodDefinition.OneTuple,
                        2 => MethodDefinition.TwoTuple,
                        3 => MethodDefinition.ThreeTuple,
                        4 => MethodDefinition.FourTuple,
                        5 => MethodDefinition.FiveTuple,
                        6 => MethodDefinition.SixTuple,
                        7 => MethodDefinition.SevenTuple,
                        8 => MethodDefinition.EightTuple,
                        _ => throw new Exception("Unexpected data parameter type."),
                    };

                    if (!parameterTypes[^1].IsGenericParameter)
                    {
                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            parameterTypes[^(genericArguments.Length - i)]
                                .Should().Be(typeof(ValueTuple<,>).MakeGenericType(typeof(string), genericArguments[i]));
                        }
                    }

                    return methodDefinition;
                })];

        System.Diagnostics.Debugger.Break();

        Assert1_7ForEachMethodName(methodDefinitions);
        Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitions);
        Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitions);
        Assert1_11ForEachDataParameterType(methodDefinitions);

        foreach (var methodDefinitionsByName in methodDefinitions.GroupBy(m => m & MethodDefinition.NameMask))
        {
            Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByName);
            Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByName);
            Assert1_11ForEachDataParameterType(methodDefinitionsByName);

            foreach (var methodDefinitionsByMessageType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.StringMessage))
            {
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByMessageType);
                Assert1_11ForEachDataParameterType(methodDefinitionsByMessageType);

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByMessageType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByEventIdAndException);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByName.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
            {
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByEventIdAndException);
                Assert1_11ForEachDataParameterType(methodDefinitionsByEventIdAndException);

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByMessageType);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.DataTypeMask))
            {
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByDataParameterType);
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByDataParameterType);

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByMessageType);
                }

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByDataParameterType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByEventIdAndException);
                }
            }
        }

        foreach (var methodDefinitionsByMessageType in methodDefinitions.GroupBy(m => m & MethodDefinition.StringMessage))
        {
            Assert1_7ForEachMethodName(methodDefinitionsByMessageType);
            Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByMessageType);
            Assert1_11ForEachDataParameterType(methodDefinitionsByMessageType);

            foreach (var methodDefinitionsByName in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.NameMask))
            {
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByName);
                Assert1_11ForEachDataParameterType(methodDefinitionsByName);

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByName.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByEventIdAndException);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByMessageType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByEventIdAndException);
                Assert1_11ForEachDataParameterType(methodDefinitionsByEventIdAndException);

                foreach (var methodDefinitionsByName in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.DataTypeMask))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByDataParameterType);
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByDataParameterType);

                foreach (var methodDefinitionsByName in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByDataParameterType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByEventIdAndException);
                }
            }
        }

        foreach (var methodDefinitionsByEventIdAndException in methodDefinitions.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
        {
            Assert1_7ForEachMethodName(methodDefinitionsByEventIdAndException);
            Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByEventIdAndException);
            Assert1_11ForEachDataParameterType(methodDefinitionsByEventIdAndException);

            foreach (var methodDefinitionsByName in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.NameMask))
            {
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByName);
                Assert1_11ForEachDataParameterType(methodDefinitionsByName);

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByMessageType);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByMessageType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.StringMessage))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByMessageType);
                Assert1_11ForEachDataParameterType(methodDefinitionsByMessageType);

                foreach (var methodDefinitionsByName in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_11ForEachDataParameterType(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.DataTypeMask))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByDataParameterType);
                }
            }

            foreach (var methodDefinitionsByDataParameterType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.DataTypeMask))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByDataParameterType);
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByDataParameterType);

                foreach (var methodDefinitionsByName in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByMessageType);
                }
            }
        }

        foreach (var methodDefinitionsByDataParameterType in methodDefinitions.GroupBy(m => m & MethodDefinition.DataTypeMask))
        {
            Assert1_7ForEachMethodName(methodDefinitionsByDataParameterType);
            Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByDataParameterType);
            Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByDataParameterType);

            foreach (var methodDefinitionsByName in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.NameMask))
            {
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByName);
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByName);

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByName.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByMessageType);
                }

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByName.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByEventIdAndException);
                }
            }

            foreach (var methodDefinitionsByMessageType in methodDefinitionsByDataParameterType.GroupBy(m => m & MethodDefinition.StringMessage))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByMessageType);
                Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByMessageType);

                foreach (var methodDefinitionsByName in methodDefinitionsByMessageType.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByMessageType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByEventIdAndException);
                }
            }

            foreach (var methodDefinitionsByEventIdAndException in methodDefinitionsByDataParameterType.GroupBy(m => m & (MethodDefinition.EventId | MethodDefinition.Exception)))
            {
                Assert1_7ForEachMethodName(methodDefinitionsByEventIdAndException);
                Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByEventIdAndException);

                foreach (var methodDefinitionsByName in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.NameMask))
                {
                    Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(methodDefinitionsByName);
                }

                foreach (var methodDefinitionsByMessageType in methodDefinitionsByEventIdAndException.GroupBy(m => m & MethodDefinition.StringMessage))
                {
                    Assert1_7ForEachMethodName(methodDefinitionsByMessageType);
                }
            }
        }

        static void Assert1_7ForEachMethodName(IEnumerable<MethodDefinition> methodDefinitions)
        {
            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Write)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Trace)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Debug)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Information)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Warning)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Error)
                .Should().HaveCount(methodDefinitions.Count() / 7);

            methodDefinitions.Where(m => (m & MethodDefinition.NameMask) == MethodDefinition.Critical)
                .Should().HaveCount(methodDefinitions.Count() / 7);
        }

        static void Assert1_2HaveStringMessage1_2HaveInterpolatedStringMessage(IEnumerable<MethodDefinition> methodDefinitions)
        {
            methodDefinitions.Where(m => (m & MethodDefinition.StringMessage) == MethodDefinition.StringMessage)
                .Should().HaveCount(methodDefinitions.Count() / 2);

            methodDefinitions.Where(m => (m & MethodDefinition.StringMessage) == MethodDefinition.None)
                .Should().HaveCount(methodDefinitions.Count() / 2);
        }

        static void Assert1_4HaveEventIdAndException1_4HaveEventId1_4HaveException1_4HaveNeither(IEnumerable<MethodDefinition> methodDefinitions)
        {
            methodDefinitions.Where(m =>
                (m & MethodDefinition.EventId) == MethodDefinition.EventId && (m & MethodDefinition.Exception) == MethodDefinition.Exception)
                .Should().HaveCount(methodDefinitions.Count() / 4);

            methodDefinitions.Where(m =>
                (m & MethodDefinition.EventId) == MethodDefinition.EventId && (m & MethodDefinition.Exception) == MethodDefinition.None)
                .Should().HaveCount(methodDefinitions.Count() / 4);

            methodDefinitions.Where(m =>
                (m & MethodDefinition.EventId) == MethodDefinition.None && (m & MethodDefinition.Exception) == MethodDefinition.Exception)
                .Should().HaveCount(methodDefinitions.Count() / 4);

            methodDefinitions.Where(m =>
                (m & MethodDefinition.EventId) == MethodDefinition.None && (m & MethodDefinition.Exception) == MethodDefinition.None)
                .Should().HaveCount(methodDefinitions.Count() / 4);
        }

        static void Assert1_11ForEachDataParameterType(IEnumerable<MethodDefinition> methodDefinitions)
        {
            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.TupleArray)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.OneTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.TwoTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.ThreeTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.FourTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.FiveTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.SixTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.SevenTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.EightTuple)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.KvpCollection)
                .Should().HaveCount(methodDefinitions.Count() / 11);

            methodDefinitions.Where(m => (m & MethodDefinition.DataTypeMask) == MethodDefinition.KvpList)
                .Should().HaveCount(methodDefinitions.Count() / 11);
        }
    }
}
