using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

[StructLayout(LayoutKind.Auto)]
internal readonly struct MessageData
{
    public readonly string? Message;
    public readonly KeyValuePairList4 InterpolationKeyValuePairs;

    public MessageData(string? message, ref readonly KeyValuePairList4 interpolationKeyValuePairs)
    {
        Message = message;
        InterpolationKeyValuePairs = interpolationKeyValuePairs;
    }

    public MessageData(string? message)
    {
        Message = message;
        InterpolationKeyValuePairs = default;
    }
}
