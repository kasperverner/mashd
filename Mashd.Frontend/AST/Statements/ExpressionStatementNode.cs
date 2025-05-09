namespace Mashd.Frontend.AST.Statements;

using Mashd.Frontend.AST.Expressions;

public class ExpressionStatementNode : StatementNode
{
    public ExpressionNode Expression { get; }

    public ExpressionStatementNode(ExpressionNode expression, int line, int column, string text)
        : base(line, column, text)
    {
        Expression = expression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitExpressionStatementNode(this);
    }
}