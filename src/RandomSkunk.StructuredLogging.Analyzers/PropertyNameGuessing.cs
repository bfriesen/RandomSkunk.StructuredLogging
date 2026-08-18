using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Guesses a structured property name for a value from the identifier(s)/method call(s) it's read
/// from - shared by <see cref="AddLogPropertyTagFormatMigration"/> (RSSL0003's fix, which always
/// falls back to a generic "PropertyName" placeholder since the guess only ever seeds a tag the
/// developer can rename) and <see cref="StructuredLoggerExtensionsInvocationMigration"/> (RSSL0005's
/// fix, which has no such fallback available - a wrong or generic name would become a permanent,
/// visible part of the rewritten message template - so it leaves the call unconverted instead when
/// nothing can be guessed).
/// </summary>
internal static class PropertyNameGuessing
{
    // A method whose name starts with one of these is treated like a property/field access for
    // guessing purposes, with the prefix stripped, e.g. GetMessage() guesses the same as a
    // property named "Message" would.
    private static readonly string[] MethodNamePrefixes = ["Get", "Create"];

    // Well-known "serialize this value to a string" methods - for a call to one of these, the
    // guess is based on the first argument's own expression instead of the method/type names,
    // e.g. JsonConvert.SerializeObject(user) guesses the same as "user" alone would.
    private static readonly (string ContainingType, string MethodName)[] SerializationMethods =
    [
        ("Newtonsoft.Json.JsonConvert", "SerializeObject"),
        ("System.Text.Json.JsonSerializer", "Serialize"),
    ];

    /// <summary>
    /// Guesses a property name for <paramref name="expression"/>, built from one or more
    /// identifier segments - each stripped of a single leading underscore and capitalized, then
    /// concatenated - or <see langword="null"/> if it can't be guessed:
    ///
    /// <list type="bullet">
    /// <item>A local or parameter by itself: just its own (treated) name, e.g. <c>who</c> guesses
    /// <c>"Who"</c>.</item>
    /// <item>A <em>static</em> field, property, or method call, however it's qualified: the
    /// (treated) containing type name followed by the (treated) member name - for a method, its
    /// name with a leading "Get"/"Create" stripped - e.g. <c>MyClass.StaticProp</c> guesses
    /// <c>"MyClassStaticProp"</c> and a static <c>MyClass.GetTotal()</c> guesses <c>"MyClassTotal"</c>.
    /// The type name is omitted when the static member's declaring type is the same type the
    /// call site itself is declared in - e.g. from inside <c>MyClass</c>, <c>StaticProp</c> or
    /// <c>MyClass.StaticProp</c> both just guess <c>"StaticProp"</c>.</item>
    /// <item>An <em>instance</em> field, property, or method call whose target is <c>this</c>
    /// (explicit or implicit): just the (treated) member name, e.g. <c>this.Field</c>, bare
    /// <c>Field</c>, or bare <c>GetMessage()</c> guesses <c>"Field"</c>/<c>"Message"</c>.</item>
    /// <item>An <em>instance</em> field, property, or method call whose target is a local or
    /// parameter: the (treated) target name followed by the (treated) member name, e.g.
    /// <c>user.Name</c> or <c>user.GetName()</c> guesses <c>"UserName"</c>.</item>
    /// <item>A nested chain of the above: every segment along the chain, each treated and
    /// concatenated in order, e.g. <c>order.Customer.Name</c> or <c>order.GetCustomer().Name</c>
    /// guesses <c>"OrderCustomerName"</c> - the method call can appear anywhere in the chain.</item>
    /// <item>A call to a well-known serialization method (<see cref="SerializationMethods"/>):
    /// guessed from its first argument instead, e.g. <c>JsonSerializer.Serialize(user)</c> guesses
    /// the same as <c>user</c> alone.</item>
    /// </list>
    ///
    /// Anything else - a method call whose name doesn't start with "Get"/"Create" and isn't a
    /// known serialization method, an indexer, a cast, or any segment along the way whose name
    /// isn't a plain identifier - can't be guessed and returns <see langword="null"/>.
    /// </summary>
    public static string? TryGuessPropertyName(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        INamedTypeSymbol? callSiteType = semanticModel.GetEnclosingSymbol(expression.SpanStart)?.ContainingType;
        List<string>? segments = TryGetSegments(expression, semanticModel, callSiteType);
        return segments is { Count: > 0 } ? string.Concat(segments) : null;
    }

    private static List<string>? TryGetSegments(ExpressionSyntax expression, SemanticModel semanticModel, ITypeSymbol? callSiteType)
    {
        if (expression is ThisExpressionSyntax)
            return [];

        if (expression is IdentifierNameSyntax identifier)
            return TryGetSymbolSegments(semanticModel.GetSymbolInfo(identifier).Symbol, callSiteType);

        if (expression is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess)
        {
            ISymbol? memberSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not (IFieldSymbol or IPropertySymbol))
                return null;

            if (memberSymbol.IsStatic)
                return TryGetStaticSegments(memberSymbol, TreatSegment(memberSymbol.Name), callSiteType);

            List<string>? targetSegments = TryGetSegments(memberAccess.Expression, semanticModel, callSiteType);
            if (targetSegments is null)
                return null;

            string? memberSegment = TreatSegment(memberSymbol.Name);
            if (memberSegment is null)
                return null;

            targetSegments.Add(memberSegment);
            return targetSegments;
        }

        if (expression is InvocationExpressionSyntax invocation)
            return TryGetInvocationSegments(invocation, semanticModel, callSiteType);

        return null;
    }

    private static List<string>? TryGetSymbolSegments(ISymbol? symbol, ITypeSymbol? callSiteType)
    {
        if (symbol is ILocalSymbol or IParameterSymbol)
            return TrySingleSegment(symbol.Name);

        if (symbol is IFieldSymbol or IPropertySymbol)
        {
            // A field/property referenced without an explicit target is either static (qualified
            // implicitly by its own type) or an instance member with an implicit `this.` target.
            return symbol.IsStatic ? TryGetStaticSegments(symbol, TreatSegment(symbol.Name), callSiteType) : TrySingleSegment(symbol.Name);
        }

        return null;
    }

    private static List<string>? TryGetInvocationSegments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ITypeSymbol? callSiteType)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return null;

        if (IsSerializationMethod(method, semanticModel.Compilation))
        {
            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            return arguments.Count == 0 ? null : TryGetSegments(arguments[0].Expression, semanticModel, callSiteType);
        }

        string? strippedName = TryStripPrefix(method.Name);
        if (strippedName is null)
            return null;

        string? memberSegment = TreatSegment(strippedName);
        if (memberSegment is null)
            return null;

        if (method.IsStatic)
            return TryGetStaticSegments(method, memberSegment, callSiteType);

        // Instance method: a receiver written in source (obj.GetX() or bare GetX(), meaning an
        // implicit `this.`) leads with the receiver's own segments, exactly like an instance
        // field/property.
        ExpressionSyntax? receiver = invocation.Expression is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } memberAccess
            ? memberAccess.Expression
            : null;

        List<string>? targetSegments = receiver is null ? [] : TryGetSegments(receiver, semanticModel, callSiteType);
        if (targetSegments is null)
            return null;

        targetSegments.Add(memberSegment);
        return targetSegments;
    }

    private static bool IsSerializationMethod(IMethodSymbol method, Compilation compilation)
    {
        if (method.ReturnType.SpecialType != SpecialType.System_String)
            return false;

        foreach ((string? typeMetadataName, string? methodName) in SerializationMethods)
        {
            if (method.Name != methodName)
                continue;

            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(typeMetadataName);
            if (type is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, type))
                return true;
        }

        return false;
    }

    private static string? TryStripPrefix(string methodName)
    {
        foreach (string prefix in MethodNamePrefixes)
        {
            if (methodName.Length > prefix.Length && methodName.StartsWith(prefix, StringComparison.Ordinal))
                return methodName.Substring(prefix.Length);
        }

        return null;
    }

    private static List<string>? TryGetStaticSegments(ISymbol memberSymbol, string? memberSegment, ITypeSymbol? callSiteType)
    {
        if (memberSegment is null)
            return null;

        INamedTypeSymbol? containingType = memberSymbol.ContainingType;
        if (containingType is null)
            return null;

        // A static member declared in the very type the call site itself is declared in doesn't
        // need its own type name repeated in the guess.
        if (callSiteType is not null && SymbolEqualityComparer.Default.Equals(containingType, callSiteType))
            return [memberSegment];

        string? typeSegment = TreatSegment(containingType.Name);
        return typeSegment is null ? null : [typeSegment, memberSegment];
    }

    private static List<string>? TrySingleSegment(string name)
    {
        string? segment = TreatSegment(name);
        return segment is null ? null : [segment];
    }

    // Strips a single leading underscore and capitalizes the first letter, or returns null if
    // "name" isn't a plain identifier (e.g. an indexer's this[]).
    private static string? TreatSegment(string name)
    {
        if (!IsSimpleIdentifier(name))
            return null;

        string strippedName = name.Length > 1 && name[0] == '_' ? name.Substring(1) : name;
        if (strippedName.Length == 0)
            return null;

        return char.IsUpper(strippedName[0]) ? strippedName : char.ToUpperInvariant(strippedName[0]) + strippedName.Substring(1);
    }

    private static bool IsSimpleIdentifier(string name) =>
        name.Length > 0 &&
        (char.IsLetter(name[0]) || name[0] == '_') &&
        name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
