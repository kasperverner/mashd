namespace Mashd.Frontend.AST.Statements;

using Mashd.Frontend.AST.Expressions;

public class ReturnNode : StatementNode
{
    public ExpressionNode Expression { get; }

    public ReturnNode(ExpressionNode expression, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Expression = expression;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitReturnNode(this);
    }
}