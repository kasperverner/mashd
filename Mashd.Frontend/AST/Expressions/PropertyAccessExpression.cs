namespace Mashd.Frontend.AST.Expressions;

public class PropertyAccessExpressionNode : ExpressionNode
{
    public ExpressionNode Left { get; }
    public string Property { get; }

    public PropertyAccessExpressionNode(ExpressionNode left, string property, int line, int column, string text)
        : base(line, column, text)
    {
        Left = left;
        Property = property;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitPropertyAccessExpressionNode(this);
    }
}