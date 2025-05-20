namespace Mashd.Frontend.AST;

public class ImportNode : AstNode
{
    
    public string Path { get; set; }

    public ImportNode(string path, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Path = path;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitImportNode(this);
    }
}