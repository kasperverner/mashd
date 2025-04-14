using System.Collections.Generic;

namespace Mashd.Frontend.AST.Expressions
{
    public class MashdSchemaField
    {
        public string Type { get; }
        public string Name { get; }

        public MashdSchemaField(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public class MashdSchemaNode : ExpressionNode
    {
        public Dictionary<string, MashdSchemaField> Fields { get; }

        public MashdSchemaNode(Dictionary<string, MashdSchemaField> fields, int line, int column, string text)
            : base(line, column, text)
        {
            Fields = fields;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitMashdSchemaNode(this);
        }
    }
}