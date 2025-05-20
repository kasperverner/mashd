namespace Mashd.Frontend.AST.Expressions
{
    public class LiteralNode : ExpressionNode
    {
        public object? Value { get; }
        
        public SymbolType ParsedType { get; set; }

        public LiteralNode(object? value, int line, int column, string text, SymbolType parsedType, int level)
            : base(line, column, text, level)
        {
            Value = value;
            ParsedType = parsedType;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitLiteralNode(this);
        }
    }
}