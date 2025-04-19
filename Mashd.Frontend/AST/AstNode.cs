namespace Mashd.Frontend.AST;

public abstract class AstNode 
{
    public int Line { get; }
    public int Column { get; }
    public string Text { get; }
    
    public SymbolType InferredType { get; set; }

    protected AstNode(int line, int column, string text)
    {
        Line = line;
        Column = column;
        Text = text;
        InferredType = SymbolType.Unknown;
    }

    public abstract T Accept<T>(IAstVisitor<T> visitor);
}
