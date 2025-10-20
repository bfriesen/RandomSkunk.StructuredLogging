using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging;

using LogAttribute = (string Key, object? Value);

internal interface ILogAttributeArray
{
    int Size { get; }

    LogAttribute Get(int index);
}

internal readonly struct LogAttributeArray(LogAttribute[]? array) : ILogAttributeArray
{
    public int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => (array ?? []).Length; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LogAttribute Get(int index) => (array ?? [])[index];
}

#if NET9_0_OR_GREATER
internal readonly struct LogAttributeArray0 : ILogAttributeArray
{
    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 0; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => throw new IndexOutOfRangeException();
}

[InlineArray(1)]
internal struct LogAttributeArray1 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 1; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(2)]
internal struct LogAttributeArray2 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 2; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(3)]
internal struct LogAttributeArray3 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 3; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(4)]
internal struct LogAttributeArray4 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 4; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(5)]
internal struct LogAttributeArray5 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 5; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(6)]
internal struct LogAttributeArray6 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 6; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(7)]
internal struct LogAttributeArray7 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 7; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}

[InlineArray(8)]
internal struct LogAttributeArray8 : ILogAttributeArray
{
    private LogAttribute _element0;

    public readonly int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => 8; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly LogAttribute Get(int index) => this[index];
}
#endif
