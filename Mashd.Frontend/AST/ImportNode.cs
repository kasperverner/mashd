namespace Mashd.Frontend.AST;

public class ImportNode : AstNode
{
    
    public string Path { get; set; }
    public string Alias { get; set; }

    public ImportNode(string path, string alias, int line, int column, string text)
        : base(line, column, text)
    {
        Path = path;
        Alias = alias;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitImportNode(this);
    }
}