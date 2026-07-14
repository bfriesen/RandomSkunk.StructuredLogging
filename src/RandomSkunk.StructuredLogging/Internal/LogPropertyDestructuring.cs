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
    private const int MaxDepth = 10;
    private const int MaxCollectionItems = 10;

    // Reflected property lists are cached per-Type since destructuring is reflection-based and a
    // given type is typically destructured repeatedly across many log calls.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>
    /// Renders <paramref name="value"/> as Serilog-style destructured text.
    /// </summary>
    public static string Render(object? value)
    {
        var sb = new StringBuilder();
        AppendValue(sb, value, depth: 0, ancestors: null);
        return sb.ToString();
    }

    private static void AppendValue(StringBuilder sb, object? value, int depth, HashSet<object>? ancestors)
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

        var type = value.GetType();

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
            AppendDictionary(sb, dictionary, type, depth, ancestors);
            return;
        }

        if (value is IEnumerable enumerable)
        {
            AppendSequence(sb, enumerable, type, depth, ancestors);
            return;
        }

        AppendObject(sb, value, type, depth, ancestors, showTypeName: !IsAnonymousType(type));
    }

    private static void AppendObject(StringBuilder sb, object value, Type type, int depth, HashSet<object>? ancestors, bool showTypeName)
    {
        if (!TryEnter(value, type, ref ancestors))
        {
            sb.Append("<circular reference>");
            return;
        }

        try
        {
            if (showTypeName)
            {
                sb.Append(GetFriendlyTypeName(type));
                sb.Append(' ');
            }

            sb.Append('{');

            var properties = PropertyCache.GetOrAdd(type, static t => t
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .ToArray());

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
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
                    var thrown = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
                    sb.Append("<getter threw ").Append(thrown.GetType().Name).Append('>');
                    continue;
                }

                AppendValue(sb, propertyValue, depth + 1, ancestors);
            }

            sb.Append(" }");
        }
        finally
        {
            Exit(value, type, ancestors);
        }
    }

    private static void AppendSequence(StringBuilder sb, IEnumerable sequence, Type type, int depth, HashSet<object>? ancestors)
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
            foreach (var item in sequence)
            {
                if (count == MaxCollectionItems)
                {
                    sb.Append(", ...");
                    break;
                }

                if (count > 0)
                    sb.Append(", ");

                AppendValue(sb, item, depth + 1, ancestors);
                count++;
            }

            sb.Append(']');
        }
        finally
        {
            Exit(sequence, type, ancestors);
        }
    }

    private static void AppendDictionary(StringBuilder sb, IDictionary dictionary, Type type, int depth, HashSet<object>? ancestors)
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
                AppendValue(sb, entry.Key, depth + 1, ancestors);
                sb.Append("]: ");
                AppendValue(sb, entry.Value, depth + 1, ancestors);
                count++;
            }

            sb.Append(" }");
        }
        finally
        {
            Exit(dictionary, type, ancestors);
        }
    }

    // Cycle tracking uses ancestor-stack semantics (add on entry, remove on exit in a `finally`),
    // not "ever visited" - so two sibling branches that happen to share a reference (not an actual
    // cycle) still both render fully. Value-type containers are skipped entirely: a struct is
    // copied by value, so it can't itself be part of a reference cycle.
    private static bool TryEnter(object value, Type type, ref HashSet<object>? ancestors)
    {
        if (!type.IsClass)
            return true;

        ancestors ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        return ancestors.Add(value);
    }

    private static void Exit(object value, Type type, HashSet<object>? ancestors)
    {
        if (type.IsClass)
            ancestors?.Remove(value);
    }

    private static void AppendQuotedString(StringBuilder sb, string value)
    {
        sb.Append('"');

        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(ch); break;
            }
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

        var name = type.Name;
        var backtickIndex = name.IndexOf('`');
        if (backtickIndex >= 0)
            name = name.Substring(0, backtickIndex);

        var typeArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
        return $"{name}<{typeArgs}>";
    }
}
