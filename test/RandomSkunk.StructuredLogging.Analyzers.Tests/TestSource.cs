namespace RandomSkunk.StructuredLogging.Analyzers.Tests;

/// <summary>
/// Builds a minimal compilable source file wrapping a single statement, shared by the analyzer
/// and code fix tests.
/// </summary>
internal static class TestSource
{
    public static string WrapInMethodBody(string statement) => $$"""
        using System;
        using Microsoft.Extensions.Logging;
        using RandomSkunk.StructuredLogging;

        namespace TestNamespace;

        public class TestClass
        {
            private const string ConstantPart = "Const";

            private object _userScore = null!;

            public object UserScore { get; set; } = null!;

            public void TestMethod(
                ILogger logger,
                object who,
                object ts,
                object userName,
                object userId,
                object ipAddress,
                object value,
                object path,
                object orderId,
                object code,
                string dynamicName)
            {
                {{statement}}
            }

            public static string GetMessage() => "message";

            public static object CreateDefault() => "default";
        }
        """;
}
