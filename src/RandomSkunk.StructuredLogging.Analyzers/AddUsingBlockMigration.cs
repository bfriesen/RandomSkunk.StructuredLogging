using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement for a call flagged by <see cref="UndisposedOperationLogAnalyzer"/>
/// (RSSL0006) that wraps the flagged statement in a <c>using</c> block with an empty body - see
/// <see cref="UndisposedOperationLogCodeFixProvider"/>. The body is deliberately left empty rather
/// than guessing which surrounding statements the developer meant to move into it; they're
/// expected to do that by hand. Offered under the same conditions as
/// <see cref="AddUsingDeclarationMigration"/>.
/// </summary>
internal static class AddUsingBlockMigration
{
    public static (StatementSyntax OldStatement, StatementSyntax NewStatement)? TryCreateReplacement(
        SemanticModel semanticModel, StatementSyntax anchorStatement, SyntaxNode invocationSyntax, IMethodSymbol method)
    {
        string? resourceText = GetResourceText(semanticModel, anchorStatement, invocationSyntax, method);
        if (resourceText is null)
            return null;

        string indentation = OperationLogFixHelpers.GetIndentation(anchorStatement);

        StatementSyntax newStatement = SyntaxFactory.ParseStatement($"using ({resourceText})\n{indentation}{{\n{indentation}}}")
            .WithLeadingTrivia(anchorStatement.GetLeadingTrivia())
            .WithTrailingTrivia(anchorStatement.GetTrailingTrivia());

        return (anchorStatement, newStatement);
    }

    private static string? GetResourceText(
        SemanticModel semanticModel, StatementSyntax anchorStatement, SyntaxNode invocationSyntax, IMethodSymbol method)
    {
        if (anchorStatement is LocalDeclarationStatementSyntax localDeclaration
            && localDeclaration.UsingKeyword.IsKind(SyntaxKind.None)
            && localDeclaration.Declaration.Variables.Count == 1
            && localDeclaration.Declaration.Variables[0].Initializer?.Value == invocationSyntax)
        {
            return localDeclaration.Declaration.ToString();
        }

        if (anchorStatement is ExpressionStatementSyntax expressionStatement
            && OperationLogFixHelpers.TryGetInvocationExpression(expressionStatement, invocationSyntax) is ExpressionSyntax invocationExpression)
        {
            string name = OperationLogFixHelpers.GetUniqueLocalName(
                semanticModel, anchorStatement.SpanStart, OperationLogFixHelpers.DefaultLocalName(method));

            return $"var {name} = {invocationExpression}";
        }

        return null;
    }
}
