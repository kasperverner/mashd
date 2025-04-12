namespace Mashd.Frontend.AST;

public abstract class AstNode 
{
    public int Line { get; }
    public int Column { get; }
    public string Text { get; }

    protected AstNode(int line, int column, string text)
    {
        Line = line;
        Column = column;
        Text = text;
    }

    public abstract T Accept<T>(IAstVisitor<T> visitor);
}
