namespace Mashd.Frontend.AST.Expressions;

public class ObjectExpressionNode : ExpressionNode
{
    public Dictionary<string, ExpressionNode> Properties { get; } = new();

    public ObjectExpressionNode(int line, int column, string text, int level)
        : base(line, column, text, level)
    {
    }
    
    public bool TryAddProperty(string key, ExpressionNode value)
    {
        return Properties.TryAdd(key, value);
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitObjectExpressionNode(this);
    }
}