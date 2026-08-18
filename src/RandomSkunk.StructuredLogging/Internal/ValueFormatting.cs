using System.Globalization;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Renders a value as plain text for journal entries (<see cref="ISubOperationLog.SetResult{T}"/>,
/// <see cref="IOperationLog.AppendValue{T}"/>) via <see cref="IFormattable"/>/<see cref="object.ToString"/>.
/// Not to be confused with <see cref="LogPropertyDestructuring"/>, the Serilog-style destructuring renderer
/// used for the <c>&lt;@PropertyName&gt;</c> message tag format.
/// </summary>
internal static class ValueFormatting
{
    public static string Format<T>(T value) => value switch
    {
        null => "null",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };
}
