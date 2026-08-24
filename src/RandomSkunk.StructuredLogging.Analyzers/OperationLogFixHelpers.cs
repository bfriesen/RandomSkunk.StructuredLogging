using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Shared helpers for <see cref="AddUsingDeclarationMigration"/> and
/// <see cref="AddUsingBlockMigration"/>, the two fixes offered by
/// <see cref="UndisposedOperationLogCodeFixProvider"/> for a call flagged by
/// <see cref="UndisposedOperationLogAnalyzer"/> (RSSL0006).
/// </summary>
internal static class OperationLogFixHelpers
{
    /// <summary>
    /// The default local variable name to introduce for the operation log returned by
    /// <paramref name="method"/>, following this project's own convention (see CLAUDE.md) of
    /// naming <c>IOperationLog</c> variables <c>log</c>/<c>subLog</c> rather than <c>op</c>.
    /// </summary>
    public static string DefaultLocalName(IMethodSymbol method) =>
        method.Name == "BeginSubOperation" ? "subLog" : "log";

    /// <summary>
    /// Returns <paramref name="baseName"/> if no symbol visible at <paramref name="position"/>
    /// already uses it, otherwise the first <c>baseName2</c>, <c>baseName3</c>, ... that isn't
    /// already in use.
    /// </summary>
    public static string GetUniqueLocalName(SemanticModel semanticModel, int position, string baseName)
    {
        ImmutableHashSet<string> existingNames = semanticModel.LookupSymbols(position)
            .Select(symbol => symbol.Name)
            .ToImmutableHashSet(StringComparer.Ordinal);

        if (!existingNames.Contains(baseName))
            return baseName;

        for (int suffix = 2; ; suffix++)
        {
            string candidate = baseName + suffix;
            if (!existingNames.Contains(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// The indentation (leading whitespace) <paramref name="statement"/> is written at, used to
    /// indent newly-synthesized lines (e.g. the braces of an empty <c>using</c> block) to match.
    /// </summary>
    public static string GetIndentation(StatementSyntax statement)
    {
        foreach (SyntaxTrivia trivia in statement.GetLeadingTrivia().Reverse())
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                return trivia.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// If <paramref name="expressionStatement"/>'s expression *is* <paramref name="invocationSyntax"/>
    /// (a bare <c>logger.BeginOperation(...);</c> call), or is a discard assignment of it
    /// (<c>_ = logger.BeginOperation(...);</c>), returns the invocation expression to build a
    /// replacement from. Otherwise (the invocation is nested inside some other expression)
    /// returns <see langword="null"/> - there's no single obviously-correct place to introduce a
    /// local in that case, so it's left for the developer to fix by hand.
    /// </summary>
    public static ExpressionSyntax? TryGetInvocationExpression(ExpressionStatementSyntax expressionStatement, SyntaxNode invocationSyntax)
    {
        if (expressionStatement.Expression == invocationSyntax)
            return expressionStatement.Expression;

        if (expressionStatement.Expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.Text: "_" } } assignment
            && assignment.Right == invocationSyntax)
        {
            return assignment.Right;
        }

        return null;
    }
}
