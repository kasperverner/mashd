using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST.Statements;

public class VariableDeclarationNode : StatementNode
{
    public VarType Type { get; }
    public string Identifier { get; }
    public AstNode Expression { get; }
    
    public bool HasInitialization { get; }

    public VariableDeclarationNode(VarType type, string identifier, ExpressionNode expression, bool hasInitialization, int line, int column, string text)
        : base(line, column, text)
    {
        Type = type;
        Identifier = identifier;
        Expression = expression;
        HasInitialization = hasInitialization;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitVariableDeclarationNode(this);
    }

}