using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomSkunk.StructuredLogging;

/// <summary>
/// Renders a value as Serilog-style destructured text for the "&lt;@PropertyName&gt;"/"&lt;@&gt;"
/// tag format (see <see cref="LogPropertyTagFormat"/>). Objects render as
/// "TypeName { Prop1: Value1, Prop2: Value2 }" (anonymous types omit the type name), collections
/// as "[item1, item2]", dictionaries as "{ [key1]: value1, [key2]: value2 }", strings/chars are
/// quoted, and other scalars (numbers, <see langword="bool"/>, enums, <see cref="DateTime"/>,
/// <see cref="Guid"/>, etc.) render unquoted via <see cref="IFormattable"/>/<see cref="object.ToString"/>
/// with <see cref="CultureInfo.InvariantCulture"/>. Recursion into nested objects/collections is
/// bounded by <see cref="MaxDepth"/>, elements per collection/dictionary are capped at
/// <see cref="MaxCollectionItems"/>, and reference-type object graphs are guarded against cycles
/// (a self-referencing object renders "&lt;circular reference&gt;" instead of recursing forever).
/// </summary>
internal static class LogPropertyDestructuring
{
    // The characters AppendQuotedString has to escape. SearchValues gives IndexOfAny a vectorized
    // scan, which is what makes bulk-copying the runs between escapes worthwhile.
    private static readonly SearchValues<char> EscapedChars = SearchValues.Create("\"\\\n\r\t");

    private const int MaxDepth = 10;
    private const int MaxCollectionItems = 10;

    // A buffer that grew past 4K came from an unusually large object graph; don't hold it on the thread
    // forever for the sake of the common, much smaller case.
    private const int MaxRetainedBufferCapacity = 4096;

    // Rendering is transient and strictly scoped - the buffer and the ancestor stack are both dead the
    // moment a value has been rendered - so rather than allocate them per call, each thread keeps one of
    // each and reuses it. Deliberately [ThreadStatic] rather than an ObjectPool: rendering a small value is
    // only a couple of hundred nanoseconds end to end, and a ConcurrentBag rent/return pair costs more than
    // that on its own, which made pooling a net loss on exactly the values that are most common. A
    // thread-static field has no contention and no bookkeeping - the same reasoning behind the BCL's own
    // StringBuilderCache. Renting detaches the instance (see Rent* below) so a reentrant render - a property
    // getter that itself logs - gets its own, and only the outermost one puts anything back.
    [ThreadStatic]
    private static StringBuilder? t_buffer;

    [ThreadStatic]
    private static List<object>? t_ancestors;

    // Reflected property lists and the display name (or null for an anonymous type, which omits
    // the type name entirely) are cached together per-Type, since destructuring is
    // reflection-based and a given type is typically destructured repeatedly across many log
    // calls - both are pure functions of Type alone, so there's no reason to redo either the
    // reflection or the (for a generic type, non-trivial) display-name string building on every
    // single call.
    private static readonly ConcurrentDictionary<Type, DestructuringTypeInfo> TypeCache = new();

    /// <summary>
    /// Renders <paramref name="value"/> as Serilog-style destructured text straight into
    /// <paramref name="handler"/>, without ever materializing the rendered text as its own
    /// <see cref="string"/> - the scratch buffer it's built in is pooled, and its contents are copied into
    /// <paramref name="handler"/> chunk by chunk. Preferred over <see cref="Render"/> wherever the rendered
    /// text is only going to be appended to a message anyway, which is every call site that doesn't need to
    /// pad it to an alignment.
    /// </summary>
    public static void AppendDestructured(ref DefaultInterpolatedStringHandler handler, object? value)
    {
        StringBuilder sb = RentBuffer();

        try
        {
            AppendRoot(sb, value);

            foreach (ReadOnlyMemory<char> chunk in sb.GetChunks())
                handler.AppendFormatted(chunk.Span);
        }
        finally
        {
            ReturnBuffer(sb);
        }
    }

    /// <summary>
    /// Renders <paramref name="value"/> as Serilog-style destructured text. Only for callers that genuinely
    /// need the text as a <see cref="string"/> (i.e. to pad it to an alignment) - anything appending it to a
    /// message should use <see cref="AppendDestructured"/> instead, which skips the intermediate string.
    /// </summary>
    public static string Render(object? value)
    {
        StringBuilder sb = RentBuffer();

        try
        {
            AppendRoot(sb, value);
            return sb.ToString();
        }
        finally
        {
            ReturnBuffer(sb);
        }
    }

    /// <summary>
    /// Renders <paramref name="value"/> into <paramref name="sb"/>, making sure the ancestor stack that
    /// <see cref="TryEnter"/> may have rented along the way is handed back afterward.
    /// </summary>
    private static void AppendRoot(StringBuilder sb, object? value)
    {
        List<object>? ancestors = null;

        try
        {
            AppendValue(sb, value, depth: 0, ref ancestors);
        }
        finally
        {
            if (ancestors is not null)
            {
                ancestors.Clear();
                t_ancestors = ancestors;
            }
        }
    }

    // Detaching on rent is what makes a reentrant render safe: if a property getter logs something that
    // itself destructures, the nested render finds the field empty and allocates its own instance rather
    // than scribbling into the buffer the outer render is still building.
    private static StringBuilder RentBuffer()
    {
        StringBuilder? sb = t_buffer;

        if (sb is null)
            return new StringBuilder();

        t_buffer = null;
        return sb;
    }

    private static void ReturnBuffer(StringBuilder sb)
    {
        if (sb.Capacity > MaxRetainedBufferCapacity)
            return;

        sb.Clear();
        t_buffer = sb;
    }

    private static void AppendValue(StringBuilder sb, object? value, int depth, ref List<object>? ancestors)
    {
        if (value is null)
        {
            sb.Append("null");
            return;
        }

        if (value is string s)
        {
            AppendQuotedString(sb, s);
            return;
        }

        if (value is char c)
        {
            AppendQuotedChar(sb, c);
            return;
        }

        Type type = value.GetType();

        if (IsScalarType(type))
        {
            sb.Append(value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString());
            return;
        }

        // Scalars/strings/chars/null are O(1) leaves and never consume depth - only genuinely
        // nested structure (objects/collections/dictionaries) is bounded here.
        if (depth >= MaxDepth)
        {
            sb.Append("...");
            return;
        }

        if (value is IDictionary dictionary)
        {
            AppendDictionary(sb, dictionary, type, depth, ref ancestors);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            AppendSequence(sb, enumerable, type, depth, ref ancestors);
            return;
        }

        AppendObject(sb, value, type, depth, ref ancestors);
    }

    private static void AppendObject(StringBuilder sb, object value, Type type, int depth, ref List<object>? ancestors)
    {
        if (!TryEnter(value, type, ref ancestors))
        {
            sb.Append("<circular reference>");
            return;
        }

        try
        {
            DestructuringTypeInfo typeInfo = TypeCache.GetOrAdd(type, static t => new DestructuringTypeInfo(
                DisplayName: IsAnonymousType(t) ? null : GetFriendlyTypeName(t),
                Properties: [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)]));

            if (typeInfo.DisplayName is not null)
            {
                sb.Append(typeInfo.DisplayName);
                sb.Append(' ');
            }

            sb.Append('{');

            PropertyInfo[] properties = typeInfo.Properties;

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                sb.Append(i == 0 ? " " : ", ");
                sb.Append(property.Name);
                sb.Append(": ");

                object? propertyValue;
                try
                {
                    propertyValue = property.GetValue(value);
                }
                catch (Exception ex)
                {
                    // A throwing property getter must never turn a log call into a crash.
                    Exception thrown = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
                    sb.Append("<getter threw ").Append(thrown.GetType().Name).Append('>');
                    continue;
                }

                AppendValue(sb, propertyValue, depth + 1, ref ancestors);
            }

            sb.Append(" }");
        }
        finally
        {
            Exit(type, ancestors);
        }
    }

    private static void AppendSequence(StringBuilder sb, IEnumerable sequence, Type type, int depth, ref List<object>? ancestors)
    {
        if (!TryEnter(sequence, type, ref ancestors))
        {
            sb.Append("<circular reference>");
            return;
        }

        try
        {
            sb.Append('[');

            int count = 0;
            foreach (object? item in sequence)
            {
                if (count == MaxCollectionItems)
                {
                    sb.Append(", ...");
                    break;
                }

                if (count > 0)
                    sb.Append(", ");

                AppendValue(sb, item, depth + 1, ref ancestors);
                count++;
            }

            sb.Append(']');
        }
        finally
        {
            Exit(type, ancestors);
        }
    }

    private static void AppendDictionary(StringBuilder sb, IDictionary dictionary, Type type, int depth, ref List<object>? ancestors)
    {
        if (!TryEnter(dictionary, type, ref ancestors))
        {
            sb.Append("<circular reference>");
            return;
        }

        try
        {
            sb.Append('{');

            int count = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (count == MaxCollectionItems)
                {
                    sb.Append(", ...");
                    break;
                }

                sb.Append(count == 0 ? " [" : ", [");
                AppendValue(sb, entry.Key, depth + 1, ref ancestors);
                sb.Append("]: ");
                AppendValue(sb, entry.Value, depth + 1, ref ancestors);
                count++;
            }

            sb.Append(" }");
        }
        finally
        {
            Exit(type, ancestors);
        }
    }

    // Cycle tracking uses ancestor-stack semantics (add on entry, remove on exit in a `finally`),
    // not "ever visited" - so two sibling branches that happen to share a reference (not an actual
    // cycle) still both render fully. Value-type containers are skipped entirely: a struct is
    // copied by value, so it can't itself be part of a reference cycle.
    //
    // A plain List scanned linearly beats a HashSet here: recursion is bounded by MaxDepth, so the stack
    // never holds more than 10 entries, and comparing that many references costs less than hashing even
    // one of them. It's also rented rather than allocated, and only on first use - a scalar, a string, or
    // a flat collection of them never touches the pool at all.
    private static bool TryEnter(object value, Type type, ref List<object>? ancestors)
    {
        if (!type.IsClass)
            return true;

        if (ancestors is null)
        {
            // Same detach-on-rent rule as the buffer, for the same reentrancy reason.
            ancestors = t_ancestors ?? new List<object>(MaxDepth);
            t_ancestors = null;
        }
        else
        {
            for (int i = 0; i < ancestors.Count; i++)
            {
                if (ReferenceEquals(ancestors[i], value))
                    return false;
            }
        }

        ancestors.Add(value);
        return true;
    }

    // Only ever called after a matching TryEnter returned true, so for a class the value being exited is
    // always the entry on top of the stack.
    private static void Exit(Type type, List<object>? ancestors)
    {
        if (type.IsClass && ancestors is { Count: > 0 })
            ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static void AppendQuotedString(StringBuilder sb, string value)
    {
        sb.Append('"');

        // Copies the runs between escapes in bulk rather than appending a character at a time. Most
        // destructured strings need no escaping at all, so the common path is a single vectorized scan
        // that finds nothing followed by one span copy.
        ReadOnlySpan<char> remaining = value;

        while (!remaining.IsEmpty)
        {
            int escapeIndex = remaining.IndexOfAny(EscapedChars);

            if (escapeIndex < 0)
            {
                sb.Append(remaining);
                break;
            }

            sb.Append(remaining[..escapeIndex]);
            sb.Append(remaining[escapeIndex] switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                // IndexOfAny matched one of EscapedChars, so nothing else can reach this arm.
                _ => "\\t",
            });

            remaining = remaining[(escapeIndex + 1)..];
        }

        sb.Append('"');
    }

    private static void AppendQuotedChar(StringBuilder sb, char value)
    {
        sb.Append('\'');

        switch (value)
        {
            case '\'': sb.Append("\\'"); break;
            case '\\': sb.Append("\\\\"); break;
            case '\n': sb.Append("\\n"); break;
            case '\r': sb.Append("\\r"); break;
            case '\t': sb.Append("\\t"); break;
            default: sb.Append(value); break;
        }

        sb.Append('\'');
    }

    private static bool IsScalarType(Type type) =>
        type.IsPrimitive ||
        type.IsEnum ||
        type == typeof(decimal) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(TimeSpan) ||
        type == typeof(DateOnly) ||
        type == typeof(TimeOnly) ||
        type == typeof(Guid) ||
        type == typeof(Uri) ||
        type == typeof(Version);

    private static bool IsAnonymousType(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) &&
        type.Name.Contains("AnonymousType");

    private static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        string name = type.Name;
        int backtickIndex = name.IndexOf('`');
        if (backtickIndex >= 0)
            name = name[..backtickIndex];

        string typeArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
        return $"{name}<{typeArgs}>";
    }

    // DisplayName is null for an anonymous type, which omits the type name from the rendered
    // output entirely - see AppendObject.
    private sealed record DestructuringTypeInfo(string? DisplayName, PropertyInfo[] Properties);
}
