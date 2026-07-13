using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement expression for a flagged <c>Microsoft.Extensions.Logging.LoggerExtensions</c>
/// call - see <see cref="AvoidLoggerExtensionsAnalyzer"/> and <see cref="AvoidLoggerExtensionsCodeFixProvider"/>.
/// </summary>
internal static class LoggerExtensionsMigration
{
    // MEL message templates only recognize simple, non-nested "{Name}" holes (no alignment or
    // format component, unlike string.Format) - matches LogValuesFormatter's own parsing.
    private static readonly Regex TemplateHolePattern = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

    // Property names captured by the <PropertyName> tag format must be simple identifiers -
    // LogPropertyTagFormat finds the tag by scanning for the next '>', so a name containing one
    // would corrupt the tag; requiring a plain identifier keeps the rewritten call unambiguous.
    private static readonly Regex ValidPropertyName = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Returns the RandomSkunk.StructuredLogging equivalent of <paramref name="invocation"/>, or
    /// <see langword="null"/> if it can't be confidently rewritten (e.g. the message isn't a
    /// string literal, or its placeholder count doesn't match the trailing argument count).
    /// </summary>
    public static ExpressionSyntax? TryCreateReplacement(IInvocationOperation invocation)
    {
        var argumentsByParameterName = new Dictionary<string, IArgumentOperation>();
        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter is not null)
                argumentsByParameterName[argument.Parameter.Name] = argument;
        }

        var receiver = invocation.Instance?.Syntax as ExpressionSyntax;
        if (receiver is null)
        {
            if (!argumentsByParameterName.TryGetValue("logger", out var loggerArgument) ||
                loggerArgument.Value.Syntax is not ExpressionSyntax loggerSyntax)
            {
                return null;
            }

            receiver = loggerSyntax;
        }

        var method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

        ExpressionSyntax? levelArgument = null;
        string newMethodName;
        if (method.Name == "Log")
        {
            if (!argumentsByParameterName.TryGetValue("logLevel", out var logLevelArgument) ||
                logLevelArgument.Value.Syntax is not ExpressionSyntax logLevelSyntax)
            {
                return null;
            }

            levelArgument = logLevelSyntax;
            newMethodName = "Write";
        }
        else
        {
            newMethodName = method.Name switch
            {
                "LogTrace" => "Trace",
                "LogDebug" => "Debug",
                "LogInformation" => "Information",
                "LogWarning" => "Warning",
                "LogError" => "Error",
                "LogCritical" => "Critical",
                _ => string.Empty,
            };

            if (newMethodName.Length == 0)
                return null;
        }

        var eventIdArgument = argumentsByParameterName.TryGetValue("eventId", out var eventIdArg)
            ? eventIdArg.Value.Syntax as ExpressionSyntax
            : null;
        var exceptionArgument = argumentsByParameterName.TryGetValue("exception", out var exceptionArg)
            ? exceptionArg.Value.Syntax as ExpressionSyntax
            : null;

        if (!argumentsByParameterName.TryGetValue("message", out var messageArgument) ||
            messageArgument.Value.Syntax is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } messageLiteral)
        {
            return null;
        }

        var formatArguments = new List<ExpressionSyntax>();
        if (argumentsByParameterName.TryGetValue("args", out var argsArgument))
        {
            if (argsArgument.ArgumentKind == ArgumentKind.ParamArray)
            {
                if (argsArgument.Value is IArrayCreationOperation { Initializer: not null } arrayCreation)
                {
                    foreach (var element in arrayCreation.Initializer.ElementValues)
                    {
                        if (element.Syntax is not ExpressionSyntax elementSyntax)
                            return null;

                        formatArguments.Add(elementSyntax);
                    }
                }
            }
            else if (argsArgument.ArgumentKind != ArgumentKind.DefaultValue)
            {
                // An array was passed directly (not as individual params values) - too
                // ambiguous to safely decompose into named holes.
                return null;
            }
        }

        var newMessage = TryBuildMessageExpression(messageLiteral, formatArguments);
        if (newMessage is null)
            return null;

        var arguments = new List<ArgumentSyntax>();
        if (levelArgument is not null)
            arguments.Add(SyntaxFactory.Argument(levelArgument.WithoutTrivia()));
        if (eventIdArgument is not null)
            arguments.Add(SyntaxFactory.Argument(eventIdArgument.WithoutTrivia()));
        if (exceptionArgument is not null)
            arguments.Add(SyntaxFactory.Argument(exceptionArgument.WithoutTrivia()));
        arguments.Add(SyntaxFactory.Argument(newMessage));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver.WithoutTrivia(),
                SyntaxFactory.IdentifierName(newMethodName)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    private static ExpressionSyntax? TryBuildMessageExpression(LiteralExpressionSyntax messageLiteral, IReadOnlyList<ExpressionSyntax> formatArguments)
    {
        var template = messageLiteral.Token.ValueText;
        var matches = TemplateHolePattern.Matches(template);
        if (matches.Count != formatArguments.Count)
            return null;

        if (matches.Count == 0)
            return messageLiteral.WithoutTrivia();

        var names = new string[matches.Count];
        for (var i = 0; i < matches.Count; i++)
        {
            var name = matches[i].Groups[1].Value;

            // MEL's "@"/"$" destructuring/stringification prefixes have no equivalent here (the
            // tag always captures the raw value) - drop the prefix and capture under the bare name.
            if (name.Length > 0 && (name[0] == '@' || name[0] == '$'))
                name = name.Substring(1);

            if (!ValidPropertyName.IsMatch(name))
                return null;

            names[i] = name;
        }

        var contents = new List<InterpolatedStringContentSyntax>();
        var position = 0;
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.Index > position)
                contents.Add(CreateText(template.Substring(position, match.Index - position)));

            var formatClause = SyntaxFactory.InterpolationFormatClause(
                SyntaxFactory.Token(SyntaxKind.ColonToken),
                SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "<" + names[i] + ">", "<" + names[i] + ">", default));

            contents.Add(SyntaxFactory.Interpolation(formatArguments[i].WithoutTrivia()).WithFormatClause(formatClause));

            position = match.Index + match.Length;
        }

        if (position < template.Length)
            contents.Add(CreateText(template.Substring(position)));

        return SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken))
            .WithContents(SyntaxFactory.List(contents))
            .WithStringEndToken(SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken));
    }

    private static InterpolatedStringTextSyntax CreateText(string text)
    {
        var escaped = EscapeForRegularInterpolatedString(text);
        return SyntaxFactory.InterpolatedStringText(
            SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, escaped, text, default));
    }

    private static string EscapeForRegularInterpolatedString(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '{': builder.Append("{{"); break;
                case '}': builder.Append("}}"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case '\0': builder.Append("\\0"); break;
                default:
                    if (char.IsControl(c))
                        builder.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }
}
