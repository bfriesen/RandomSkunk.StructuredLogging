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

    /// <summary>
    /// Reported on an interpolation hole in a RandomSkunk.StructuredLogging message argument that
    /// uses the <c>&lt;PropertyName&gt;</c> tag format to capture the interpolated value as a
    /// structured property. This diagnostic is "silent" (<see cref="DiagnosticSeverity.Hidden"/>)
    /// by design - it exists purely as an anchor location for code fixes (added separately) that
    /// operate on these holes, not to flag anything wrong with the code.
    /// </summary>
    public static readonly DiagnosticDescriptor LogPropertyTagFormatInterpolationHole = new(
        id: "RSSL0002",
        title: "Interpolation hole captures a structured property",
        messageFormat: "This interpolation hole captures '{0}' as a structured property using the '<PropertyName>' tag format",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Marks an interpolation hole (e.g. {who:<Recipient>} in $\"Hello, {who:<Recipient>}!\") in a RandomSkunk.StructuredLogging message argument that captures its value as a structured property via the <PropertyName> tag format. Reported at silent/hidden severity since it isn't a warning about anything wrong with the code - it exists so that code fixes can target these holes.");

    /// <summary>
    /// Reported on an interpolation hole in a RandomSkunk.StructuredLogging message argument that
    /// does <em>not</em> use the <c>&lt;PropertyName&gt;</c> tag format to capture the interpolated
    /// value as a structured property. This diagnostic is "silent" (<see cref="DiagnosticSeverity.Hidden"/>)
    /// by design - it exists purely as an anchor location for code fixes (added separately) that
    /// operate on these holes, not to flag anything wrong with the code.
    /// </summary>
    public static readonly DiagnosticDescriptor NonCapturingInterpolationHole = new(
        id: "RSSL0003",
        title: "Interpolation hole does not capture a structured property",
        messageFormat: "This interpolation hole does not capture '{0}' as a structured property",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Marks an interpolation hole (e.g. {who} in $\"Hello, {who}!\") in a RandomSkunk.StructuredLogging message argument whose value is not captured as a structured property via the <PropertyName> tag format. Reported at silent/hidden severity since it isn't a warning about anything wrong with the code - it exists so that code fixes can target these holes.");

    /// <summary>
    /// Reported on a name/value tuple argument (e.g. <c>("UserId", userId)</c>) passed at the end
    /// of a RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write
    /// extension method call to attach a structured property. This diagnostic is "silent"
    /// (<see cref="DiagnosticSeverity.Hidden"/>) by design - it exists purely as an anchor
    /// location for code fixes (added separately) that operate on these arguments, not to flag
    /// anything wrong with the code.
    /// </summary>
    public static readonly DiagnosticDescriptor LogPropertyTupleArgument = new(
        id: "RSSL0004",
        title: "Tuple argument passes a structured property",
        messageFormat: "This name/value tuple argument passes '{0}' as a structured property named '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Marks a name/value tuple argument (e.g. (\"UserId\", userId) in logger.Debug($\"...\", (\"UserId\", userId))) passed at the end of a RandomSkunk.StructuredLogging extension method call to attach a structured property, when the name is a compile-time constant string (a literal, a constant concatenation, or an interpolated string whose holes are themselves constant strings). Reported at silent/hidden severity since it isn't a warning about anything wrong with the code - it exists so that code fixes can target these arguments.");

    /// <summary>
    /// Reported on a call to any of the RandomSkunk.StructuredLogging
    /// Trace/Debug/Information/Warning/Error/Critical/Write extension methods. This diagnostic is
    /// "silent" (<see cref="DiagnosticSeverity.Hidden"/>) by design - it exists purely as an anchor
    /// location for code fixes (added separately) that operate on these calls, not to flag anything
    /// wrong with the code.
    /// </summary>
    public static readonly DiagnosticDescriptor StructuredLoggerExtensionsInvocation = new(
        id: "RSSL0005",
        title: "Call to a RandomSkunk.StructuredLogging extension method",
        messageFormat: "This is a call to the RandomSkunk.StructuredLogging '{0}' extension method",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Marks a call to one of the RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write extension methods. Reported at silent/hidden severity since it isn't a warning about anything wrong with the code - it exists so that code fixes can target these calls.");
}
