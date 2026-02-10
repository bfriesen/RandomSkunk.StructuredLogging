using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines an immutable array-like structure of key-value pairs.
/// </summary>
internal interface IKeyValuePairArray
{
    int Length { get; }

    KeyValuePair<string, object?> this[int index] { get; }
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by an array of <see langword="object"/> log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeArray((string Key, object? Value)[]? logAttributes) : IKeyValuePairArray
{
    private readonly (string Key, object? Value)[] _logAttributes = logAttributes ?? [];

    public int Length => _logAttributes.Length;

    public KeyValuePair<string, object?> this[int index] => new(_logAttributes[index].Key, _logAttributes[index].Value);
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by a single strongly-typed log attribute tuple.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T>(ref readonly (string Key, T Value) logAttribute) : IKeyValuePairArray
{
    private readonly string _logAttributeKey = logAttribute.Key;
    private readonly T _logAttributeValue = logAttribute.Value;

    public readonly int Length => 1;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttributeKey, _logAttributeValue),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by two strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;

    public readonly int Length => 2;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by three strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;

    public readonly int Length => 3;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by four strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;

    public readonly int Length => 4;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by five strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;
    private readonly string _logAttribute5Key = logAttribute5.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;
    private readonly T5 _logAttribute5Value = logAttribute5.Value;

    public readonly int Length => 5;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by six strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;
    private readonly string _logAttribute5Key = logAttribute5.Key;
    private readonly string _logAttribute6Key = logAttribute6.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;
    private readonly T5 _logAttribute5Value = logAttribute5.Value;
    private readonly T6 _logAttribute6Value = logAttribute6.Value;

    public readonly int Length => 6;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            5 => new(_logAttribute6Key, _logAttribute6Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by seven strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6,
    ref readonly (string Key, T7 Value) logAttribute7) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;
    private readonly string _logAttribute5Key = logAttribute5.Key;
    private readonly string _logAttribute6Key = logAttribute6.Key;
    private readonly string _logAttribute7Key = logAttribute7.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;
    private readonly T5 _logAttribute5Value = logAttribute5.Value;
    private readonly T6 _logAttribute6Value = logAttribute6.Value;
    private readonly T7 _logAttribute7Value = logAttribute7.Value;

    public readonly int Length => 7;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            5 => new(_logAttribute6Key, _logAttribute6Value),
            6 => new(_logAttribute7Key, _logAttribute7Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs backed by eight strongly-typed log attribute tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6,
    ref readonly (string Key, T7 Value) logAttribute7,
    ref readonly (string Key, T8 Value) logAttribute8) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;
    private readonly string _logAttribute5Key = logAttribute5.Key;
    private readonly string _logAttribute6Key = logAttribute6.Key;
    private readonly string _logAttribute7Key = logAttribute7.Key;
    private readonly string _logAttribute8Key = logAttribute8.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;
    private readonly T5 _logAttribute5Value = logAttribute5.Value;
    private readonly T6 _logAttribute6Value = logAttribute6.Value;
    private readonly T7 _logAttribute7Value = logAttribute7.Value;
    private readonly T8 _logAttribute8Value = logAttribute8.Value;

    public readonly int Length => 8;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            5 => new(_logAttribute6Key, _logAttribute6Value),
            6 => new(_logAttribute7Key, _logAttribute7Value),
            7 => new(_logAttribute8Key, _logAttribute8Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of key-value pairs initialized from a collection of key-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct KeyValuePairCollection : IKeyValuePairArray
{
    private readonly KeyValuePairList8 _collectionKeyValuePairs;

    public KeyValuePairCollection(IReadOnlyCollection<KeyValuePair<string, object?>>? keyValuePairs)
    {
        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
                _collectionKeyValuePairs.Add(kvp);
        }
    }

    public int Length => _collectionKeyValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] => _collectionKeyValuePairs[index];
}
