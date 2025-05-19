namespace Mashd.Frontend.AST.Expressions;

public class TypeLiteralNode : ExpressionNode
{
    public SymbolType Type { get; }
        
    public SymbolType ParsedType { get; set; }

    public TypeLiteralNode(SymbolType type, int line, int column, string text, SymbolType parsedType, int level)
        : base(line, column, text, level)
    {
        Type = type;
        ParsedType = parsedType;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitTypeLiteralNode(this);
    }
}