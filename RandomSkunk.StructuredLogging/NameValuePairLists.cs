using System.Collections;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Defines an expandable list of name-value pairs optimized for small counts (up to 2 items before heap allocation).
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal struct NameValuePairList2 : IEnumerable<KeyValuePair<string, object?>>
{
    private int _count;
    private KeyValuePair<string, object?> _item0;
    private KeyValuePair<string, object?> _item1;
    private List<KeyValuePair<string, object?>>? _overflow;

    public readonly int Count => _count;

    public readonly KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException();

            return index switch
            {
                0 => _item0,
                1 => _item1,
                _ => _overflow![index - 2],
            };
        }
    }

    public void Add(KeyValuePair<string, object?> item)
    {
        switch (_count++)
        {
            case 0:
                _item0 = item;
                break;
            case 1:
                _item1 = item;
                break;
            case 2:
                _overflow = new(4) { item };
                break;
            default:
                _overflow!.Add(item);
                break;
        }
    }

    readonly IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return this[i];
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)this).GetEnumerator();
}

/// <summary>
/// Defines an expandable list of name-value pairs optimized for small counts (up to 6 items before heap allocation).
/// </summary>
[StructLayout(LayoutKind.Auto)]
internal struct NameValuePairList6 : IEnumerable<KeyValuePair<string, object?>>
{
    private int _count;
    private KeyValuePair<string, object?> _item0;
    private KeyValuePair<string, object?> _item1;
    private KeyValuePair<string, object?> _item2;
    private KeyValuePair<string, object?> _item3;
    private KeyValuePair<string, object?> _item4;
    private KeyValuePair<string, object?> _item5;
    private List<KeyValuePair<string, object?>>? _overflow;

    public readonly int Count => _count;

    public readonly KeyValuePair<string, object?> this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new IndexOutOfRangeException();

            return index switch
            {
                0 => _item0,
                1 => _item1,
                2 => _item2,
                3 => _item3,
                4 => _item4,
                5 => _item5,
                _ => _overflow![index - 6],
            };
        }
    }

    public void Add(KeyValuePair<string, object?> item)
    {
        switch (_count++)
        {
            case 0:
                _item0 = item;
                break;
            case 1:
                _item1 = item;
                break;
            case 2:
                _item2 = item;
                break;
            case 3:
                _item3 = item;
                break;
            case 4:
                _item4 = item;
                break;
            case 5:
                _item5 = item;
                break;
            case 6:
                _overflow = new(4) { item };
                break;
            default:
                _overflow!.Add(item);
                break;
        }
    }

    readonly IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
            yield return this[i];
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)this).GetEnumerator();
}
