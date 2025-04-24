using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST.Definitions
{
    public class SchemaField
    {
        public string Type { get; }
        public string Name { get; }

        public SchemaField(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public class SchemaObjectNode : ExpressionNode
    {
        public Dictionary<string, SchemaField> Fields { get; }

        public SchemaObjectNode(Dictionary<string, SchemaField> fields, int line, int column, string text)
            : base(line, column, text)
        {
            Fields = fields;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitSchemaObjectNode(this);
        }
    }
}