using System.Collections;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Represents an empty, read-only array of name/value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct EmptyNameValuePairArray : IReadOnlyList<KeyValuePair<string, object?>>
{
    /// <inheritdoc/>
    public int Count => 0;

    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] => throw new IndexOutOfRangeException();

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<EmptyNameValuePairArray> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by an array of <see langword="object"/> log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyArray : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly (string Name, object? Value)[] _logProperties;

    internal LogPropertyArray((string Name, object? Value)[]? logProperties)
    {
        _logProperties = logProperties ?? [];
    }

    /// <inheritdoc/>
    public int Count => _logProperties.Length;

    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] => GetKeyValuePair(_logProperties[index]);

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyArray> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static KeyValuePair<string, object?> GetKeyValuePair((string Name, object? Value) logProperty) =>
        new(logProperty.Name, logProperty.Value);
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by a single strongly-typed log property tuple.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;
    private readonly string _logPropertyKey;
    private readonly T _logPropertyValue;

    internal LogPropertyTuple(ref readonly (string Name, T Value) logProperty)
    {
        _count = 1;
        _logPropertyKey = logProperty.Name;
        _logPropertyValue = logProperty.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logPropertyKey, _logPropertyValue),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by two strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2)
    {
        _count = 2;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logProperty1Key, _logProperty1Value),
                1 => new(_logProperty2Key, _logProperty2Value),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by three strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3)
    {
        _count = 3;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logProperty1Key, _logProperty1Value),
                1 => new(_logProperty2Key, _logProperty2Value),
                2 => new(_logProperty3Key, _logProperty3Value),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by four strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3, T4> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;
    private readonly string _logProperty4Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;
    private readonly T4 _logProperty4Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4)
    {
        _count = 4;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty4Key = logProperty4.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
        _logProperty4Value = logProperty4.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logProperty1Key, _logProperty1Value),
                1 => new(_logProperty2Key, _logProperty2Value),
                2 => new(_logProperty3Key, _logProperty3Value),
                3 => new(_logProperty4Key, _logProperty4Value),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3, T4>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by five strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3, T4, T5> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;
    private readonly string _logProperty4Key;
    private readonly string _logProperty5Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;
    private readonly T4 _logProperty4Value;
    private readonly T5 _logProperty5Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5)
    {
        _count = 5;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty4Key = logProperty4.Name;
        _logProperty5Key = logProperty5.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
        _logProperty4Value = logProperty4.Value;
        _logProperty5Value = logProperty5.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logProperty1Key, _logProperty1Value),
                1 => new(_logProperty2Key, _logProperty2Value),
                2 => new(_logProperty3Key, _logProperty3Value),
                3 => new(_logProperty4Key, _logProperty4Value),
                4 => new(_logProperty5Key, _logProperty5Value),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3, T4, T5>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by six strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;
    private readonly string _logProperty4Key;
    private readonly string _logProperty5Key;
    private readonly string _logProperty6Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;
    private readonly T4 _logProperty4Value;
    private readonly T5 _logProperty5Value;
    private readonly T6 _logProperty6Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6)
    {
        _count = 6;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty4Key = logProperty4.Name;
        _logProperty5Key = logProperty5.Name;
        _logProperty6Key = logProperty6.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
        _logProperty4Value = logProperty4.Value;
        _logProperty5Value = logProperty5.Value;
        _logProperty6Value = logProperty6.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
            {
                0 => new(_logProperty1Key, _logProperty1Value),
                1 => new(_logProperty2Key, _logProperty2Value),
                2 => new(_logProperty3Key, _logProperty3Value),
                3 => new(_logProperty4Key, _logProperty4Value),
                4 => new(_logProperty5Key, _logProperty5Value),
                5 => new(_logProperty6Key, _logProperty6Value),
                _ => throw new IndexOutOfRangeException(),
            };

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3, T4, T5, T6>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by seven strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;
    private readonly string _logProperty4Key;
    private readonly string _logProperty5Key;
    private readonly string _logProperty6Key;
    private readonly string _logProperty7Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;
    private readonly T4 _logProperty4Value;
    private readonly T5 _logProperty5Value;
    private readonly T6 _logProperty6Value;
    private readonly T7 _logProperty7Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6,
        ref readonly (string Name, T7 Value) logProperty7)
    {
        _count = 7;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty4Key = logProperty4.Name;
        _logProperty5Key = logProperty5.Name;
        _logProperty6Key = logProperty6.Name;
        _logProperty7Key = logProperty7.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
        _logProperty4Value = logProperty4.Value;
        _logProperty5Value = logProperty5.Value;
        _logProperty6Value = logProperty6.Value;
        _logProperty7Value = logProperty7.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
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

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by eight strongly-typed log property tuples.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8> : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly byte _count;

    private readonly string _logProperty1Key;
    private readonly string _logProperty2Key;
    private readonly string _logProperty3Key;
    private readonly string _logProperty4Key;
    private readonly string _logProperty5Key;
    private readonly string _logProperty6Key;
    private readonly string _logProperty7Key;
    private readonly string _logProperty8Key;

    private readonly T1 _logProperty1Value;
    private readonly T2 _logProperty2Value;
    private readonly T3 _logProperty3Value;
    private readonly T4 _logProperty4Value;
    private readonly T5 _logProperty5Value;
    private readonly T6 _logProperty6Value;
    private readonly T7 _logProperty7Value;
    private readonly T8 _logProperty8Value;

    internal LogPropertyTuple(
        ref readonly (string Name, T1 Value) logProperty1,
        ref readonly (string Name, T2 Value) logProperty2,
        ref readonly (string Name, T3 Value) logProperty3,
        ref readonly (string Name, T4 Value) logProperty4,
        ref readonly (string Name, T5 Value) logProperty5,
        ref readonly (string Name, T6 Value) logProperty6,
        ref readonly (string Name, T7 Value) logProperty7,
        ref readonly (string Name, T8 Value) logProperty8)
    {
        _count = 8;
        _logProperty1Key = logProperty1.Name;
        _logProperty2Key = logProperty2.Name;
        _logProperty3Key = logProperty3.Name;
        _logProperty4Key = logProperty4.Name;
        _logProperty5Key = logProperty5.Name;
        _logProperty6Key = logProperty6.Name;
        _logProperty7Key = logProperty7.Name;
        _logProperty8Key = logProperty8.Name;
        _logProperty1Value = logProperty1.Value;
        _logProperty2Value = logProperty2.Value;
        _logProperty3Value = logProperty3.Value;
        _logProperty4Value = logProperty4.Value;
        _logProperty5Value = logProperty5.Value;
        _logProperty6Value = logProperty6.Value;
        _logProperty7Value = logProperty7.Value;
        _logProperty8Value = logProperty8.Value;
    }

    /// <inheritdoc/>
    public readonly int Count => _count;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> this[int index] =>
        _count == 0
            ? throw new IndexOutOfRangeException()
            : index switch
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

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<LogPropertyTuple<T1, T2, T3, T4, T5, T6, T7, T8>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs initialized from a read-only collection of name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ReadOnlyNameValuePairCollection : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly NameValuePairList6 _nameValuePairs;

    internal ReadOnlyNameValuePairCollection(IReadOnlyCollection<KeyValuePair<string, object?>>? nameValuePairCollection)
    {
        if (nameValuePairCollection != null)
        {
            foreach (var kvp in nameValuePairCollection)
                _nameValuePairs.Add(kvp);
        }
    }

    /// <inheritdoc/>
    public int Count => _nameValuePairs.Count;

    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] => _nameValuePairs[index];

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<ReadOnlyNameValuePairCollection> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// An immutable array-like structure of name-value pairs backed by a read-only list of name-value pairs.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ReadOnlyNameValuePairList<TNameValuePairList> : IReadOnlyList<KeyValuePair<string, object?>>
    where TNameValuePairList : IReadOnlyList<KeyValuePair<string, object?>>
{
    private readonly TNameValuePairList? _logProperties;

    internal ReadOnlyNameValuePairList(TNameValuePairList? nameValuePairList) => _logProperties = nameValuePairList;

    /// <inheritdoc/>
    public int Count => _logProperties?.Count ?? 0;

    /// <inheritdoc/>
    public KeyValuePair<string, object?> this[int index] =>
        _logProperties == null
            ? throw new IndexOutOfRangeException()
            : _logProperties[index];

    /// <summary>
    /// Returns an enumerator that iterates through the collection of name-value pairs.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection of name-value pairs.</returns>
    public NameValuePairListEnumerator<ReadOnlyNameValuePairList<TNameValuePairList>> GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Enumerates the elements of a read-only list of name/value pairs, providing sequential access to each key and value.
/// </summary>
/// <typeparam name="TNameValuePairList">The type of the read-only list containing name/value pairs to enumerate. Must implement
/// IReadOnlyList&lt;KeyValuePair&lt;string, object?&gt;&gt; and cannot be null.</typeparam>
public struct NameValuePairListEnumerator<TNameValuePairList>
    : IEnumerator<KeyValuePair<string, object?>>
    where TNameValuePairList : struct, IReadOnlyList<KeyValuePair<string, object?>>
{
    private TNameValuePairList _nameValuePairList;
    private int _index = -1;

    internal NameValuePairListEnumerator(TNameValuePairList nameValuePairArray) =>
        _nameValuePairList = nameValuePairArray;

    /// <inheritdoc/>
    public readonly KeyValuePair<string, object?> Current => _nameValuePairList[_index];

    readonly object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext() => ++_index < _nameValuePairList.Count;

    /// <inheritdoc/>
    public void Dispose() => this = default;

    void IEnumerator.Reset() => throw new NotSupportedException();
}
