using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST.Statements;

public class TernaryNode : ExpressionNode
{
    public ExpressionNode Condition { get; }
    public ExpressionNode TrueExpression { get; }
    public ExpressionNode FalseExpression { get; }
    public TernaryNode(ExpressionNode condition, ExpressionNode trueExpression, ExpressionNode falseExpression, int line, int column, string text)
        : base(line, column, text)
    {
        Condition = condition;
        TrueExpression = trueExpression;
        FalseExpression = falseExpression;
    }
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitTernaryNode(this);
    }
}