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
            public void TestMethod(
                ILogger logger,
                object who,
                object userName,
                object userId,
                object ipAddress,
                object value,
                object path,
                object orderId,
                object code)
            {
                {{statement}}
            }

            public static string GetMessage() => "message";
        }
        """;
}
