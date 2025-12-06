using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

internal interface IKeyValuePairArray
{
    int Length { get; }

    KeyValuePair<string, object?> this[int index] { get; }
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeArray(
    (string Key, object? Value)[]? logAttributes,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
{
    private readonly (string Key, object? Value)[] _logAttributes = logAttributes ?? [];
    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public int Length => _logAttributes.Length + _interpolationKeyValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] =>
        index < _logAttributes.Length
            ? new(_logAttributes[index].Key, _logAttributes[index].Value)
            : _interpolationKeyValuePairs[index - _logAttributes.Length];
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T>(
    ref readonly (string Key, T Value) logAttribute,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
{
    private readonly string _logAttributeKey = logAttribute.Key;
    private readonly T _logAttributeValue = logAttribute.Value;

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 1 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttributeKey, _logAttributeValue),
            _ => _interpolationKeyValuePairs[index - 1],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 2 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            _ => _interpolationKeyValuePairs[index - 2],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 3 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            _ => _interpolationKeyValuePairs[index - 3],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
{
    private readonly string _logAttribute1Key = logAttribute1.Key;
    private readonly string _logAttribute2Key = logAttribute2.Key;
    private readonly string _logAttribute3Key = logAttribute3.Key;
    private readonly string _logAttribute4Key = logAttribute4.Key;

    private readonly T1 _logAttribute1Value = logAttribute1.Value;
    private readonly T2 _logAttribute2Value = logAttribute2.Value;
    private readonly T3 _logAttribute3Value = logAttribute3.Value;
    private readonly T4 _logAttribute4Value = logAttribute4.Value;

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 4 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            _ => _interpolationKeyValuePairs[index - 4],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
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

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 5 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            _ => _interpolationKeyValuePairs[index - 5],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
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

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 6 + _interpolationKeyValuePairs.Count;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logAttribute1Key, _logAttribute1Value),
            1 => new(_logAttribute2Key, _logAttribute2Value),
            2 => new(_logAttribute3Key, _logAttribute3Value),
            3 => new(_logAttribute4Key, _logAttribute4Value),
            4 => new(_logAttribute5Key, _logAttribute5Value),
            5 => new(_logAttribute6Key, _logAttribute6Value),
            _ => _interpolationKeyValuePairs[index - 6],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6,
    ref readonly (string Key, T7 Value) logAttribute7,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
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

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 7 + _interpolationKeyValuePairs.Count;

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
            _ => _interpolationKeyValuePairs[index - 7],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct LogAttributeTuple<T1, T2, T3, T4, T5, T6, T7, T8>(
    ref readonly (string Key, T1 Value) logAttribute1,
    ref readonly (string Key, T2 Value) logAttribute2,
    ref readonly (string Key, T3 Value) logAttribute3,
    ref readonly (string Key, T4 Value) logAttribute4,
    ref readonly (string Key, T5 Value) logAttribute5,
    ref readonly (string Key, T6 Value) logAttribute6,
    ref readonly (string Key, T7 Value) logAttribute7,
    ref readonly (string Key, T8 Value) logAttribute8,
    KeyValuePairList4 interpolationKeyValuePairs) : IKeyValuePairArray
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

    private readonly KeyValuePairList4 _interpolationKeyValuePairs = interpolationKeyValuePairs;

    public readonly int Length => 8 + _interpolationKeyValuePairs.Count;

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
            _ => _interpolationKeyValuePairs[index - 8],
        };
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct KeyValuePairCollection : IKeyValuePairArray
{
    private readonly KeyValuePairList8 _collectionKeyValuePairs;
    private readonly KeyValuePairList4 _interpolationKeyValuePairs;

    public KeyValuePairCollection(
        IReadOnlyCollection<KeyValuePair<string, object?>>? keyValuePairs,
        KeyValuePairList4 interpolationKeyValuePairs)
    {
        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
                _collectionKeyValuePairs.Add(kvp);
        }

        _interpolationKeyValuePairs = interpolationKeyValuePairs;
    }

    public int Length => _collectionKeyValuePairs.Count + _interpolationKeyValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] =>
        index < _collectionKeyValuePairs.Count
            ? _collectionKeyValuePairs[index]
            : _interpolationKeyValuePairs[index - _collectionKeyValuePairs.Count];
}
