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

    /// <summary>
    /// Reported on a call to <c>BeginOperation</c>/<c>BeginSubOperation</c> whose returned
    /// <c>IOperationLog</c> isn't visibly disposed. Forgetting to dispose it means no log entry
    /// (not even a partial one) is ever written for the operation.
    /// </summary>
    public static readonly DiagnosticDescriptor UndisposedOperationLog = new(
        id: "RSSL0006",
        title: "Dispose the operation log",
        messageFormat: "The IOperationLog returned by '{0}' should be disposed, typically with a 'using' declaration or statement, so its journal is written as a log entry",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "BeginOperation/BeginSubOperation return an IOperationLog whose disposal is what actually writes its log entry (root) or 'complete' journal line (sub-operation). Nothing enforces disposal, and forgetting it silently drops the entire journal - no exception, no partial log entry - and leaks the operation's pooled StringBuilder. This is the operation-logging analog of CA2000, which can't catch this itself: its escape analysis anchors on 'new' expressions visible in the consuming compilation, and BeginOperation's internal object construction is opaque, living inside the already-compiled library assembly.");

    /// <summary>
    /// Reported on a local variable that's initialized from an interpolated string expression and
    /// then passed as the plain <c>string message</c> argument of a
    /// RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write call.
    /// </summary>
    public static readonly DiagnosticDescriptor InterpolatedStringAssignedToLocalMessageArgument = new(
        id: "RSSL0007",
        title: "Don't assign an interpolated message to a local before logging",
        messageFormat: "'{0}' is initialized from an interpolated string and then passed as this call's message argument; this binds the plain 'string' overload instead of the interpolated-string-handler overload, so the message is always built eagerly (even when this level is disabled) and any '<PropertyName>' tag is handed to the value's IFormattable.ToString(format) as a real format string instead of being parsed as a property tag - pass the interpolated string directly to the logging call instead",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The disabled-level optimization and the <PropertyName> tag format both depend on an interpolated string literal binding directly to a logging call's interpolated-string-handler overload. Assigning the interpolated string to a string local first (e.g. `string msg = $\"User {id:<UserId>}\"; logger.Debug(msg);`) forces the plain `string message` overload instead: the string is built eagerly regardless of level, and any <PropertyName> tag is handed to the interpolated value's IFormattable.ToString(format) as a genuine .NET format string, which usually throws FormatException at the log call site the first time that code path runs.");

    /// <summary>
    /// Reported on an interpolation hole in a RandomSkunk.StructuredLogging message argument whose
    /// format specifier starts with '&lt;' but has no matching '&gt;' - almost always a missing
    /// '&gt;' typo, since a real format that needs to start with a literal '&lt;' should use the
    /// <c>&lt;&gt;</c> escape hatch instead. At run time this throws
    /// <c>UnterminatedLogPropertyTagException</c>; this diagnostic catches the same mistake at
    /// compile time instead.
    /// </summary>
    public static readonly DiagnosticDescriptor UnterminatedLogPropertyTag = new(
        id: "RSSL0008",
        title: "Unterminated '<PropertyName>' tag",
        messageFormat: "This interpolation hole's format starts with '<' but has no matching '>', so it isn't a valid '<PropertyName>' tag; use the '<>' escape hatch if the format is meant to start with a literal '<'",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A format specifier that starts with '<' but never closes with '>' (e.g. {value:<UserId}, a missing '>' typo) can't be parsed as a <PropertyName> tag or the <> no-capture escape hatch. At run time, RandomSkunk.StructuredLogging throws UnterminatedLogPropertyTagException rather than silently treating the raw text as a real format string handed to IFormattable.ToString(format). This diagnostic flags the same mistake at compile time. If the format is genuinely meant to start with a literal '<', use the '<>' escape hatch, e.g. {value:<><realformat}.");
}
