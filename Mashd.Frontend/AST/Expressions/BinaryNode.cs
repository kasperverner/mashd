namespace Mashd.Frontend.AST.Expressions;

public class BinaryNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }
    public OpType Operator { get; }
    
    public BinaryNode(ExpressionNode left, ExpressionNode right, OpType op, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Left = left;
        Right = right;
        Operator = op;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBinaryNode(this);
    }
}