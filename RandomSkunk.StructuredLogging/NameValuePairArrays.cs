using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines an immutable array-like structure of name-value pairs.
/// </summary>
internal interface INameValuePairArray
{
    int Length { get; }

    KeyValuePair<string, object?> this[int index] { get; }
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by an array of <see langword="object"/> log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyArray((string Key, object? Value)[]? logProperties) : INameValuePairArray
{
    private readonly (string Key, object? Value)[] _logProperties = logProperties ?? [];

    public int Length => _logProperties.Length;

    public KeyValuePair<string, object?> this[int index] => new(_logProperties[index].Key, _logProperties[index].Value);
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by a single strongly-typed log property tuple.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T>(ref readonly (string Key, T Value) logProperty) : INameValuePairArray
{
    private readonly string _logPropertyKey = logProperty.Key;
    private readonly T _logPropertyValue = logProperty.Value;

    public readonly int Length => 1;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logPropertyKey, _logPropertyValue),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by two strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;

    public readonly int Length => 2;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by three strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;

    public readonly int Length => 3;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by four strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3, T4>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3,
    ref readonly (string Key, T4 Value) logProperty4) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;
    private readonly string _logProperty4Key = logProperty4.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;
    private readonly T4 _logProperty4Value = logProperty4.Value;

    public readonly int Length => 4;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            3 => new(_logProperty4Key, _logProperty4Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by five strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3, T4, T5>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3,
    ref readonly (string Key, T4 Value) logProperty4,
    ref readonly (string Key, T5 Value) logProperty5) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;
    private readonly string _logProperty4Key = logProperty4.Key;
    private readonly string _logProperty5Key = logProperty5.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;
    private readonly T4 _logProperty4Value = logProperty4.Value;
    private readonly T5 _logProperty5Value = logProperty5.Value;

    public readonly int Length => 5;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            3 => new(_logProperty4Key, _logProperty4Value),
            4 => new(_logProperty5Key, _logProperty5Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by six strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3,
    ref readonly (string Key, T4 Value) logProperty4,
    ref readonly (string Key, T5 Value) logProperty5,
    ref readonly (string Key, T6 Value) logProperty6) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;
    private readonly string _logProperty4Key = logProperty4.Key;
    private readonly string _logProperty5Key = logProperty5.Key;
    private readonly string _logProperty6Key = logProperty6.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;
    private readonly T4 _logProperty4Value = logProperty4.Value;
    private readonly T5 _logProperty5Value = logProperty5.Value;
    private readonly T6 _logProperty6Value = logProperty6.Value;

    public readonly int Length => 6;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            3 => new(_logProperty4Key, _logProperty4Value),
            4 => new(_logProperty5Key, _logProperty5Value),
            5 => new(_logProperty6Key, _logProperty6Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by seven strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3,
    ref readonly (string Key, T4 Value) logProperty4,
    ref readonly (string Key, T5 Value) logProperty5,
    ref readonly (string Key, T6 Value) logProperty6,
    ref readonly (string Key, T7 Value) logProperty7) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;
    private readonly string _logProperty4Key = logProperty4.Key;
    private readonly string _logProperty5Key = logProperty5.Key;
    private readonly string _logProperty6Key = logProperty6.Key;
    private readonly string _logProperty7Key = logProperty7.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;
    private readonly T4 _logProperty4Value = logProperty4.Value;
    private readonly T5 _logProperty5Value = logProperty5.Value;
    private readonly T6 _logProperty6Value = logProperty6.Value;
    private readonly T7 _logProperty7Value = logProperty7.Value;

    public readonly int Length => 7;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            3 => new(_logProperty4Key, _logProperty4Value),
            4 => new(_logProperty5Key, _logProperty5Value),
            5 => new(_logProperty6Key, _logProperty6Value),
            6 => new(_logProperty7Key, _logProperty7Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by eight strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>(
    ref readonly (string Key, T1 Value) logProperty1,
    ref readonly (string Key, T2 Value) logProperty2,
    ref readonly (string Key, T3 Value) logProperty3,
    ref readonly (string Key, T4 Value) logProperty4,
    ref readonly (string Key, T5 Value) logProperty5,
    ref readonly (string Key, T6 Value) logProperty6,
    ref readonly (string Key, T7 Value) logProperty7,
    ref readonly (string Key, T8 Value) logProperty8) : INameValuePairArray
{
    private readonly string _logProperty1Key = logProperty1.Key;
    private readonly string _logProperty2Key = logProperty2.Key;
    private readonly string _logProperty3Key = logProperty3.Key;
    private readonly string _logProperty4Key = logProperty4.Key;
    private readonly string _logProperty5Key = logProperty5.Key;
    private readonly string _logProperty6Key = logProperty6.Key;
    private readonly string _logProperty7Key = logProperty7.Key;
    private readonly string _logProperty8Key = logProperty8.Key;

    private readonly T1 _logProperty1Value = logProperty1.Value;
    private readonly T2 _logProperty2Value = logProperty2.Value;
    private readonly T3 _logProperty3Value = logProperty3.Value;
    private readonly T4 _logProperty4Value = logProperty4.Value;
    private readonly T5 _logProperty5Value = logProperty5.Value;
    private readonly T6 _logProperty6Value = logProperty6.Value;
    private readonly T7 _logProperty7Value = logProperty7.Value;
    private readonly T8 _logProperty8Value = logProperty8.Value;

    public readonly int Length => 8;

    public readonly KeyValuePair<string, object?> this[int index] =>
        index switch
        {
            0 => new(_logProperty1Key, _logProperty1Value),
            1 => new(_logProperty2Key, _logProperty2Value),
            2 => new(_logProperty3Key, _logProperty3Value),
            3 => new(_logProperty4Key, _logProperty4Value),
            4 => new(_logProperty5Key, _logProperty5Value),
            5 => new(_logProperty6Key, _logProperty6Value),
            6 => new(_logProperty7Key, _logProperty7Value),
            7 => new(_logProperty8Key, _logProperty8Value),
            _ => throw new IndexOutOfRangeException(),
        };
}

/// <summary>
/// An immutable array-like structure of name-value pairs initialized from a read-only collection of name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ReadOnlyNameValuePairCollection : INameValuePairArray
{
    private readonly NameValuePairList8 _nameValuePairs;

    public ReadOnlyNameValuePairCollection(IReadOnlyCollection<KeyValuePair<string, object?>>? nameValuePairCollection)
    {
        if (nameValuePairCollection != null)
        {
            foreach (var kvp in nameValuePairCollection)
                _nameValuePairs.Add(kvp);
        }
    }

    public int Length => _nameValuePairs.Count;

    public KeyValuePair<string, object?> this[int index] => _nameValuePairs[index];
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by a read-only list of name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ReadOnlyNameValuePairList(IReadOnlyList<KeyValuePair<string, object?>>? nameValuePairList) : INameValuePairArray
{
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _nameValuePairList = nameValuePairList ?? [];

    public int Length => _nameValuePairList.Count;

    public KeyValuePair<string, object?> this[int index] => _nameValuePairList[index];
}
