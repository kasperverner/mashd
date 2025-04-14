namespace Mashd.Frontend.AST.Expressions;

public class DatasetLiteralNode : ExpressionNode
{
    public class DatasetProperty
    {
        public string Key { get; }
        public object Value { get; }

        public DatasetProperty(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }

    public Dictionary<string, DatasetProperty> Properties { get; }

    public DatasetLiteralNode(int line, int column, string text, Dictionary<string, DatasetProperty> properties)
        : base(line, column, text)
    {
        Properties = properties;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitDatasetLiteralNode(this);
    }
}