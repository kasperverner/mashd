using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST.Definitions;

public class DatasetObjectNode : ExpressionNode
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
    public SchemaDefinitionNode? ResolvedSchema { get; set; }

    public DatasetObjectNode(int line, int column, string text, Dictionary<string, DatasetProperty> properties)
        : base(line, column, text)
    {
        Properties = properties;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitDatasetObjectNode(this);
    }
}