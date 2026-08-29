using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Renders a value as plain text for journal entries (<see cref="ISubOperationLog.AppendResult{T}"/>,
/// <see cref="ISubOperationLog.AppendValue{T}"/>) via <see cref="IFormattable"/>/<see cref="object.ToString"/>,
/// or appends it as indented JSON (<see cref="ISubOperationLog.AppendJson{T}"/>). Not to be confused with
/// <see cref="LogPropertyDestructuring"/>, the Serilog-style destructuring renderer used for the
/// <c>&lt;@PropertyName&gt;</c> message tag format.
/// </summary>
internal static class ValueFormatting
{
    /// <summary>
    /// Appends <paramref name="value"/> to <paramref name="journal"/> as plain text, rendered via
    /// <see cref="IFormattable"/>/<see cref="object.ToString"/> with
    /// <see cref="CultureInfo.InvariantCulture"/>, or the literal <c>null</c> when it has no text of its
    /// own. Appends rather than returning a <see cref="string"/> so that a value the journal is only going
    /// to absorb anyway never becomes an intermediate allocation: for the common case of an
    /// <see cref="ISpanFormattable"/> value (every built-in numeric type, <see cref="DateTime"/>,
    /// <see cref="Guid"/>, ...) the interpolated string handler formats it straight into
    /// <paramref name="journal"/>'s current chunk, with neither a boxed value nor a rendered string.
    /// </summary>
    public static void AppendValue<T>(StringBuilder journal, T value)
    {
        if (value is null)
        {
            journal.Append("null");
            return;
        }

        if (value is IFormattable)
        {
            journal.Append(CultureInfo.InvariantCulture, $"{value}");
            return;
        }

        // Not IFormattable, so there's nothing to format into - ToString() is the only rendering available,
        // and a type whose ToString() returns null still reads as "null" in the journal.
        journal.Append(value.ToString() ?? "null");
    }

    /// <summary>
    /// Serializes <paramref name="value"/> as indented JSON directly into <paramref name="journal"/>,
    /// without ever materializing the JSON as its own <see cref="string"/>. Writes UTF-8 bytes via a
    /// pooled <see cref="Utf8JsonWriter"/> (see <see cref="OperationLogPools.JsonWriters"/>), then
    /// transcodes those bytes to UTF-16 through an <see cref="ArrayPool{T}"/>-rented <see langword="char"/>
    /// buffer appended straight to <paramref name="journal"/>.
    /// </summary>
    public static void AppendJson<T>(StringBuilder journal, T value)
    {
        PooledJsonWriter pooled = OperationLogPools.JsonWriters.Rent();

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
        int maxCharCount = Encoding.UTF8.GetMaxCharCount(utf8.Length);
        char[] chars = ArrayPool<char>.Shared.Rent(maxCharCount);

        try
        {
            int charCount = Encoding.UTF8.GetChars(utf8, chars);
            journal.Append(chars, 0, charCount);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }
}
