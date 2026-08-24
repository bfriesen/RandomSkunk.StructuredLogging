using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RandomSkunk.StructuredLogging.Analyzers;

/// <summary>
/// Builds the replacement for a call flagged by <see cref="UndisposedOperationLogAnalyzer"/>
/// (RSSL0006) that turns the flagged statement into a <c>using</c> declaration - see
/// <see cref="UndisposedOperationLogCodeFixProvider"/>. Offered only when the invocation is either
/// the entire expression of an expression statement (including a discarded <c>_ = ...;</c>
/// assignment) or the initializer of a single-variable local declaration that doesn't already use
/// <c>using</c> - anything else (the invocation nested inside a larger expression, e.g. passed as
/// an argument) is left for the developer to fix by hand, since there's no single
/// obviously-correct place to introduce the local.
/// </summary>
internal static class AddUsingDeclarationMigration
{
    public static (StatementSyntax OldStatement, StatementSyntax NewStatement)? TryCreateReplacement(
        SemanticModel semanticModel, StatementSyntax anchorStatement, SyntaxNode invocationSyntax, IMethodSymbol method)
    {
        if (anchorStatement is LocalDeclarationStatementSyntax localDeclaration
            && localDeclaration.UsingKeyword.IsKind(SyntaxKind.None)
            && localDeclaration.Declaration.Variables.Count == 1
            && localDeclaration.Declaration.Variables[0].Initializer?.Value == invocationSyntax)
        {
            // Strip the original leading trivia (indentation) first so it doesn't end up
            // duplicated on both the new `using` keyword and the type that follows it.
            LocalDeclarationStatementSyntax updated = localDeclaration
                .WithLeadingTrivia()
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithLeadingTrivia(localDeclaration.GetLeadingTrivia());

            return (localDeclaration, updated);
        }

        if (anchorStatement is ExpressionStatementSyntax expressionStatement
            && OperationLogFixHelpers.TryGetInvocationExpression(expressionStatement, invocationSyntax) is ExpressionSyntax invocationExpression)
        {
            string name = OperationLogFixHelpers.GetUniqueLocalName(
                semanticModel, anchorStatement.SpanStart, OperationLogFixHelpers.DefaultLocalName(method));

            VariableDeclarationSyntax declaration = SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name))
                            .WithInitializer(SyntaxFactory.EqualsValueClause(invocationExpression.WithoutTrivia()))))
                .NormalizeWhitespace();

            LocalDeclarationStatementSyntax newDeclaration = SyntaxFactory.LocalDeclarationStatement(declaration)
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
                .WithTrailingTrivia(expressionStatement.GetTrailingTrivia());

            return (expressionStatement, newDeclaration);
        }

        return null;
    }
}
