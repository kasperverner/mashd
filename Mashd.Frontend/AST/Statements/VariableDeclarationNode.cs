using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Statements;

public class VariableDeclarationNode : StatementNode, IDeclaration
{
    public SymbolType DeclaredType { get; }
    public string Identifier { get; }
    public AstNode? Expression { get; }

    public VariableDeclarationNode(SymbolType type, string identifier, ExpressionNode? expression, int line, int column, string text)
        : base(line, column, text)
    {
        DeclaredType = type;
        Identifier = identifier;
        Expression = expression;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitVariableDeclarationNode(this);
    }

}