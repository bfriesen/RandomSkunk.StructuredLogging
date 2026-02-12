using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines a structured log entry's message and any name-value pairs extracted during string interpolation.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct MessageData(string? message, in NameValuePairList4 interpolationNameValuePairs = default)
{
    public readonly string? Message = message;
    public readonly NameValuePairList4 InterpolationNameValuePairs = interpolationNameValuePairs;
}
