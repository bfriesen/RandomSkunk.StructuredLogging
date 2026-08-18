using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement expression for a call flagged by <see cref="StructuredLoggerExtensionsInvocationAnalyzer"/>
/// (RSSL0005) - see <see cref="StructuredLoggerExtensionsInvocationCodeFixProvider"/>. Rewrites a
/// RandomSkunk.StructuredLogging Trace/Debug/Information/Warning/Error/Critical/Write call into the
/// "roughly equivalent" Microsoft.Extensions.Logging LogTrace/LogDebug/.../Log call - the inverse of
/// <see cref="LoggerExtensionsMigration"/> (RSSL0001), but necessarily lossy in places since MEL's
/// message-template placeholders are the only way it can carry a named structured property:
///
/// <list type="bullet">
/// <item>A <c>&lt;PropertyName&gt;</c> tag-format hole becomes a <c>{PropertyName}</c> (or
/// <c>{PropertyName:format}</c>, if the tag has a residual format) placeholder in place, with the
/// raw value passed as a trailing positional argument.</item>
/// <item>A <c>&lt;@PropertyName&gt;</c> destructuring tag becomes <c>{@PropertyName}</c> - only
/// truly destructured if the app's actual logging provider understands that syntax (e.g. Serilog);
/// base MEL treats '@' as a literal name character.</item>
/// <item>An interpolation hole with no tag - including a bare <c>&lt;&gt;</c>/<c>&lt;@&gt;</c>
/// opt-out tag - isn't captured today either, but MEL has no way to format a value into the message
/// without it also becoming a named property, so one is guessed via <see cref="PropertyNameGuessing"/>
/// (the same guess <see cref="AddLogPropertyTagFormatMigration"/> (RSSL0003's fix) uses to seed a
/// new tag) and the hole is turned into a placeholder exactly like an explicit tag would be. Unlike
/// that fix, there's no generic fallback name available here - a wrong or generic name would become
/// a permanent, visible part of the rewritten message - so if nothing can be guessed for a given
/// hole, the whole call is left unconverted instead.</item>
/// <item>An explicit (string Name, T Value) property - from a generic-arity argument - with a
/// compile-time-constant name that isn't already embedded in the message via a tag becomes a
/// trailing <c>{PropertyName}</c> placeholder appended to the message text, separated by a single
/// space (no comma), with the value passed as a trailing positional argument.</item>
/// </list>
///
/// A call is also left unconverted (this method returns <see langword="null"/>) when it contains a
/// property whose name can't be pinned down at compile time - a tuple argument with a non-constant
/// name, or any use of the leading <c>IReadOnlyCollection&lt;KeyValuePair&lt;string, object?&gt;&gt;</c>
/// collection-parameter overload, whose keys are only known at run time - or a message argument
/// that isn't a string literal or interpolated string.
/// </summary>
internal static class StructuredLoggerExtensionsInvocationMigration
{
    private static readonly string[] GenericPropertyParameterNames =
        ["logProperty1", "logProperty2", "logProperty3", "logProperty4", "logProperty5", "logProperty6"];

    /// <summary>
    /// Returns the Microsoft.Extensions.Logging equivalent of <paramref name="invocation"/>, or
    /// <see langword="null"/> if it can't be confidently (or completely) converted - see the type
    /// summary for the specific cases that bail out.
    /// </summary>
    public static InvocationExpressionSyntax? TryCreateReplacement(IInvocationOperation invocation, SemanticModel semanticModel)
    {
        Dictionary<string, IArgumentOperation> argumentsByParameterName = new();
        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter is not null)
                argumentsByParameterName[argument.Parameter.Name] = argument;
        }

        ExpressionSyntax? receiver = invocation.Instance?.Syntax as ExpressionSyntax;
        if (receiver is null)
        {
            if (!argumentsByParameterName.TryGetValue("logger", out IArgumentOperation? loggerArgument) ||
                loggerArgument.Value.Syntax is not ExpressionSyntax loggerSyntax)
            {
                return null;
            }

            receiver = loggerSyntax;
        }

        IMethodSymbol method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        ExpressionSyntax? levelArgument = null;
        string newMethodName;
        if (method.Name == "Write")
        {
            if (!argumentsByParameterName.TryGetValue("level", out IArgumentOperation? levelArg) ||
                levelArg.Value.Syntax is not ExpressionSyntax levelSyntax)
            {
                return null;
            }

            levelArgument = levelSyntax;
            newMethodName = "Log";
        }
        else
        {
            newMethodName = method.Name switch
            {
                "Trace" => "LogTrace",
                "Debug" => "LogDebug",
                "Information" => "LogInformation",
                "Warning" => "LogWarning",
                "Error" => "LogError",
                "Critical" => "LogCritical",
                _ => string.Empty,
            };

            if (newMethodName.Length == 0)
                return null;
        }

        ExpressionSyntax? eventIdArgument = argumentsByParameterName.TryGetValue("eventId", out IArgumentOperation? eventIdArg)
            ? eventIdArg.Value.Syntax as ExpressionSyntax
            : null;
        ExpressionSyntax? exceptionArgument = argumentsByParameterName.TryGetValue("exception", out IArgumentOperation? exceptionArg)
            ? exceptionArg.Value.Syntax as ExpressionSyntax
            : null;

        if (!argumentsByParameterName.TryGetValue("message", out IArgumentOperation? messageArgument) ||
            messageArgument.Value.Syntax is not ExpressionSyntax messageSyntax)
        {
            return null;
        }

        List<(string Name, ExpressionSyntax Value)>? explicitProperties = CollectExplicitProperties(method, argumentsByParameterName, semanticModel);
        if (explicitProperties is null)
            return null;

        (ExpressionSyntax Message, List<ExpressionSyntax> Args)? built = messageSyntax switch
        {
            InterpolatedStringExpressionSyntax interpolatedString => BuildFromInterpolatedString(interpolatedString, explicitProperties, semanticModel),
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal => BuildFromLiteral(literal, explicitProperties),
            _ => null,
        };

        if (built is null)
            return null;

        (ExpressionSyntax? newMessage, List<ExpressionSyntax>? trailingArgs) = built.Value;

        List<ArgumentSyntax> arguments = new();
        if (levelArgument is not null)
            arguments.Add(SyntaxFactory.Argument(levelArgument.WithoutTrivia()));
        if (eventIdArgument is not null)
            arguments.Add(SyntaxFactory.Argument(eventIdArgument.WithoutTrivia()));
        if (exceptionArgument is not null)
            arguments.Add(SyntaxFactory.Argument(exceptionArgument.WithoutTrivia()));
        arguments.Add(SyntaxFactory.Argument(newMessage));
        arguments.AddRange(trailingArgs.Select(SyntaxFactory.Argument));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver.WithoutTrivia(),
                SyntaxFactory.IdentifierName(newMethodName)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    /// <summary>
    /// Gathers the explicit (string Name, T Value) structured properties passed to the call - from
    /// generic-arity <c>logProperty1..6</c> arguments - resolving each name's compile-time constant
    /// value. Returns <see langword="null"/> if any property's name can't be pinned down at compile
    /// time, or if the call uses the leading collection-parameter overload (whose keys are only known
    /// at run time).
    /// </summary>
    private static List<(string Name, ExpressionSyntax Value)>? CollectExplicitProperties(
        IMethodSymbol method, Dictionary<string, IArgumentOperation> argumentsByParameterName, SemanticModel semanticModel)
    {
        List<(string Name, ExpressionSyntax Value)> properties = new();

        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (!argumentsByParameterName.TryGetValue(parameter.Name, out IArgumentOperation? argument))
                continue;

            if (parameter.Name == "logProperties")
            {
                // The leading IReadOnlyCollection<KeyValuePair<string, object?>> overload - its
                // keys are only known at run time, so it can never become a {Name} placeholder.
                return null;
            }

            if (GenericPropertyParameterNames.Contains(parameter.Name))
            {
                if (!TryAddTupleProperty(argument.Value.Syntax, properties, semanticModel))
                    return null;
            }
        }

        return properties;
    }

    private static bool TryAddTupleProperty(
        SyntaxNode? syntax, List<(string Name, ExpressionSyntax Value)> properties, SemanticModel semanticModel)
    {
        if (syntax is not TupleExpressionSyntax { Arguments.Count: 2 } tuple)
            return false;

        string? name = ConstantStringExpressionParsing.TryGetConstantStringValue(tuple.Arguments[0].Expression, semanticModel);
        if (name is null || !IsSafePlaceholderName(name))
            return false;

        properties.Add((name, tuple.Arguments[1].Expression));
        return true;
    }

    private static (ExpressionSyntax Message, List<ExpressionSyntax> Args)? BuildFromInterpolatedString(
        InterpolatedStringExpressionSyntax interpolatedString, List<(string Name, ExpressionSyntax Value)> explicitProperties,
        SemanticModel semanticModel)
    {
        List<InterpolatedStringContentSyntax> contents = new();
        List<ExpressionSyntax> args = new();

        foreach (InterpolatedStringContentSyntax content in interpolatedString.Contents)
        {
            if (content is not InterpolationSyntax interpolation)
            {
                contents.Add(content);
                continue;
            }

            string? formatText = interpolation.FormatClause?.FormatStringToken.ValueText;
            bool isTag = !string.IsNullOrEmpty(formatText) && formatText![0] == '<';

            string? propertyName = isTag ? LogPropertyTagFormatParsing.TryGetPropertyName(formatText!) : null;
            bool isDestructuring = isTag && LogPropertyTagFormatParsing.IsDestructuring(formatText!);
            string? residualFormat = isTag ? LogPropertyTagFormatParsing.GetRemainingFormat(formatText!) : formatText;

            if (propertyName is null)
            {
                // Not captured: no tag at all, or an empty "<>"/"<@>" opt-out tag. MEL has no way
                // to format a value into the message without it also becoming a named property, so
                // guess one from the expression - the same guess RSSL0003's fix uses to seed a new
                // tag. Unlike that fix, there's no generic fallback available here (a wrong name
                // would become a permanent, visible part of the message), so a failed guess bails
                // out of the whole call rather than leaving this one hole unconverted.
                propertyName = PropertyNameGuessing.TryGuessPropertyName(interpolation.Expression, semanticModel);
                if (propertyName is null)
                    return null;
            }

            if (!IsSafePlaceholderName(propertyName))
                return null;

            string placeholderName = isDestructuring ? "@" + propertyName : propertyName;
            if (isDestructuring)
                residualFormat = null;
            else if (residualFormat is not null && !IsSafePlaceholderName(residualFormat))
                return null;

            string placeholderText = residualFormat is null ? $"{{{placeholderName}}}" : $"{{{placeholderName}:{residualFormat}}}";
            contents.Add(CreateTextPiece(placeholderText));
            args.Add(interpolation.Expression.WithoutTrivia());
        }

        foreach ((string? name, ExpressionSyntax? value) in explicitProperties)
        {
            contents.Add(CreateTextPiece($" {{{name}}}"));
            args.Add(value.WithoutTrivia());
        }

        // Every interpolation hole above either turned into placeholder text or bailed out the
        // whole call, so nothing but plain text pieces ever reaches here - the result always
        // collapses to a single ordinary string literal, never a genuine interpolated string.
        return (CollapseToLiteral(contents), args);
    }

    private static (ExpressionSyntax Message, List<ExpressionSyntax> Args)? BuildFromLiteral(
        LiteralExpressionSyntax literal, List<(string Name, ExpressionSyntax Value)> explicitProperties)
    {
        if (explicitProperties.Count == 0)
            return (literal.WithoutTrivia(), []);

        foreach ((string? name, ExpressionSyntax _) in explicitProperties)
        {
            if (!IsSafePlaceholderName(name))
                return null;
        }

        string text = literal.Token.ValueText + string.Concat(explicitProperties.Select(p => $" {{{p.Name}}}"));
        LiteralExpressionSyntax newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text));
        List<ExpressionSyntax> args = explicitProperties.Select(p => p.Value.WithoutTrivia()).ToList();

        return (newLiteral, args);
    }

    // A name or residual format containing '{' or '}' would corrupt the single outer "{...}"
    // placeholder it's embedded in - conservatively bail rather than emit malformed template text.
    private static bool IsSafePlaceholderName(string text) => text.IndexOf('{') < 0 && text.IndexOf('}') < 0;

    private static ExpressionSyntax CollapseToLiteral(List<InterpolatedStringContentSyntax> contents)
    {
        string text = string.Concat(contents.OfType<InterpolatedStringTextSyntax>().Select(t => t.TextToken.ValueText));
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(text));
    }

    private static InterpolatedStringTextSyntax CreateTextPiece(string rawValue)
    {
        string escapedText = SymbolDisplay.FormatLiteral(rawValue, quote: false).Replace("{", "{{").Replace("}", "}}");
        SyntaxToken textToken = SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, escapedText, rawValue, default);
        return SyntaxFactory.InterpolatedStringText(textToken);
    }
}
