using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Renders a value as plain text for journal entries (<see cref="ISubOperationLog.SetResult{T}"/>,
/// <see cref="IOperationLog.AppendValue{T}"/>) via <see cref="IFormattable"/>/<see cref="object.ToString"/>,
/// or appends it as indented JSON (<see cref="IOperationLog.AppendJson{T}"/>). Not to be confused with
/// <see cref="LogPropertyDestructuring"/>, the Serilog-style destructuring renderer used for the
/// <c>&lt;@PropertyName&gt;</c> message tag format.
/// </summary>
internal static class ValueFormatting
{
    public static string Format<T>(T value) => value switch
    {
        null => "null",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };

    /// <summary>
    /// Serializes <paramref name="value"/> as indented JSON directly into <paramref name="journal"/>,
    /// without ever materializing the JSON as its own <see cref="string"/>. Writes UTF-8 bytes via a
    /// pooled <see cref="Utf8JsonWriter"/> (see <see cref="OperationLogPools.JsonWriters"/>), then
    /// transcodes those bytes to UTF-16 through an <see cref="ArrayPool{T}"/>-rented <see langword="char"/>
    /// buffer appended straight to <paramref name="journal"/>.
    /// </summary>
    public static void AppendJson<T>(StringBuilder journal, T value)
    {
        var pooled = OperationLogPools.JsonWriters.Rent();

        try
        {
            JsonSerializer.Serialize(pooled.Writer, value);
            pooled.Writer.Flush();

            AppendUtf8(journal, pooled.WrittenSpan);
        }
        finally
        {
            OperationLogPools.JsonWriters.Return(pooled);
        }
    }

    private static void AppendUtf8(StringBuilder journal, ReadOnlySpan<byte> utf8)
    {
        var maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8.Length);
        var chars = ArrayPool<char>.Shared.Rent(maxCharCount);

        try
        {
            var charCount = Encoding.UTF8.GetChars(utf8, chars);
            journal.Append(chars, 0, charCount);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }
}
