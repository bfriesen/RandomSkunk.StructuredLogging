using Microsoft.CodeAnalysis;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Diagnostic descriptors reported by this assembly's analyzers.
/// </summary>
public static class DiagnosticDescriptors
{
    /// <summary>
    /// Reported on a call to one of <c>Microsoft.Extensions.Logging.LoggerExtensions</c>'
    /// <c>Log</c>/<c>LogTrace</c>/<c>LogDebug</c>/<c>LogInformation</c>/<c>LogWarning</c>/
    /// <c>LogError</c>/<c>LogCritical</c> extension methods.
    /// </summary>
    public static readonly DiagnosticDescriptor AvoidLoggerExtensionsMethod = new(
        id: "RSSL0001",
        title: "Prefer a RandomSkunk.StructuredLogging extension method",
        messageFormat: "Prefer a RandomSkunk.StructuredLogging extension method over '{0}', which forces structured properties into the message template",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Microsoft.Extensions.Logging.LoggerExtensions' Log/LogTrace/LogDebug/LogInformation/LogWarning/LogError/LogCritical extension methods force every structured property into the message template. RandomSkunk.StructuredLogging's Trace/Debug/Information/Warning/Error/Critical/Write extension methods let the message be worded freely while structured properties are attached separately.");
}
