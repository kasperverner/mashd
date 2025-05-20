namespace Mashd.Frontend.AST;

public abstract class AstNode 
{
    public int Level { get; }
    public int Line { get; }
    public int Column { get; }
    public string Text { get; }
    
    public SymbolType InferredType { get; set; }

    protected AstNode(int line, int column, string text, int level)
    {
        Line = line;
        Column = column;
        Text = text;
        Level = level;
        InferredType = SymbolType.Unknown;
    }

    public abstract T Accept<T>(IAstVisitor<T> visitor);
}
