namespace Mashd.Frontend.AST.Expressions;

public class ObjectExpressionNode : ExpressionNode
{
    public List<KeyValuePair> Pairs { get; }

    public ObjectExpressionNode(List<KeyValuePair> pairs, int line, int column, string text)
        : base(line, column, text)
    {
        Pairs = pairs;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitObjectExpressionNode(this);
    }

    public class KeyValuePair
    {
        public string Key { get; }
        public ExpressionNode Value { get; }

        public KeyValuePair(string key, ExpressionNode value)
        {
            Key = key;
            Value = value;
        }
    }
}