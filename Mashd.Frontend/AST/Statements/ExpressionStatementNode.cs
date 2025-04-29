using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST.Statements;

public class ExpressionStatementNode : StatementNode
{
    public ExpressionNode Expression { get; set; }
    
    public ExpressionStatementNode(ExpressionNode expression, int line, int column, string text) : base(line, column, text)
    {
        Expression = expression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitExpressionStatementNode(this);
    }
}