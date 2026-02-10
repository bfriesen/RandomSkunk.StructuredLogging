using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines a structured log entry's message and any key-value pairs extracted during string interpolation.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct MessageData(string? message, in KeyValuePairList4 interpolationKeyValuePairs = default)
{
    public readonly string? Message = message;
    public readonly KeyValuePairList4 InterpolationKeyValuePairs = interpolationKeyValuePairs;
}
