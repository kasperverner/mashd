namespace Mashd.Frontend.AST.Expressions
{
    public class LiteralNode : ExpressionNode
    {
        public object Value { get; }

        public LiteralNode(object value, int line, int column, string text)
            : base(line, column, text)
        {
            Value = value;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitLiteralNode(this);
        }
    }
}