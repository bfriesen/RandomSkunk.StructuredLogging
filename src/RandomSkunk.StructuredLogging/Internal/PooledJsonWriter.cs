using System.Buffers;
using System.Text.Json;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Pairs an <see cref="ArrayBufferWriter{T}"/> of <see cref="byte"/> with a <see cref="Utf8JsonWriter"/>
/// bound to it, so a single pooled instance (see <see cref="OperationLogPools.JsonWriters"/>) can be
/// reused across <see cref="ValueFormatting.AppendJson{T}"/> calls without allocating a new byte buffer or
/// writer each time - only <see cref="Reset"/> (via <see cref="ArrayBufferWriter{T}.Clear"/> and
/// <see cref="Utf8JsonWriter.Reset()"/>) is needed between uses, the documented pattern for reusing a
/// <see cref="Utf8JsonWriter"/>.
/// </summary>
internal sealed class PooledJsonWriter
{
    private readonly ArrayBufferWriter<byte> _buffer = new(initialCapacity: 256);

    public readonly Utf8JsonWriter Writer;

    public PooledJsonWriter() => Writer = new Utf8JsonWriter(_buffer, new JsonWriterOptions { Indented = true });

    /// <summary>The UTF-8 bytes written so far. Only valid after <see cref="Utf8JsonWriter.Flush"/>.</summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer.WrittenSpan;

    public int Capacity => _buffer.Capacity;

    public void Reset()
    {
        _buffer.Clear();
        Writer.Reset();
    }
}
