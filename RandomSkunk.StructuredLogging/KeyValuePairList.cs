using System.Collections;
using System.Runtime.InteropServices;

namespace RandomSkunk.StructuredLogging;

[StructLayout(LayoutKind.Auto)]
internal struct KeyValuePairList4 : IEnumerable<KeyValuePair<string, object?>>
{
    private int _count;
    private KeyValuePair<string, object?> _item0;
    private KeyValuePair<string, object?> _item1;
    private KeyValuePair<string, object?> _item2;
    private KeyValuePair<string, object?> _item3;
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
                _ => _overflow![index - 4],
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
                _overflow = [item];
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

[StructLayout(LayoutKind.Auto)]
internal struct KeyValuePairList8 : IEnumerable<KeyValuePair<string, object?>>
{
    private int _count;
    private KeyValuePair<string, object?> _item0;
    private KeyValuePair<string, object?> _item1;
    private KeyValuePair<string, object?> _item2;
    private KeyValuePair<string, object?> _item3;
    private KeyValuePair<string, object?> _item4;
    private KeyValuePair<string, object?> _item5;
    private KeyValuePair<string, object?> _item6;
    private KeyValuePair<string, object?> _item7;
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
                6 => _item6,
                7 => _item7,
                _ => _overflow![index - 8],
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
                _item6 = item;
                break;
            case 7:
                _item7 = item;
                break;
            case 8:
                _overflow = [item];
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
