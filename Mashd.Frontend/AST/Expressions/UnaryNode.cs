namespace Mashd.Frontend.AST.Expressions;

public class UnaryNode: ExpressionNode
{
    public ExpressionNode Operand { get; }
    public OpType Operator { get; }
    
    public UnaryNode(ExpressionNode operand, OpType unaryOperator, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Operand = operand;
        Operator = unaryOperator;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitUnaryNode(this);
    }
}