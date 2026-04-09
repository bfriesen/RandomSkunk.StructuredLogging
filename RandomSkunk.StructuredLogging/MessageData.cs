using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines a structured log entry's message and additional name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct MessageData(string? message, in NameValuePairList2 additionalNameValuePairs = default)
{
    public readonly string? Message = message;
    public readonly NameValuePairList2 AdditionalNameValuePairs = additionalNameValuePairs;
}
