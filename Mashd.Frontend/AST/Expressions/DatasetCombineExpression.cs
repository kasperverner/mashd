namespace Mashd.Frontend.AST.Expressions;

public class DatasetCombineExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }

    public DatasetCombineExpressionNode(ExpressionNode left, ExpressionNode right, int line, int column, string text)
        : base(line, column, text)
    {
        Left = left;
        Right = right;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitDatasetCombineExpressionNode(this);
    }
}