namespace Mashd.Frontend.AST.Definitions;

public class FormalParameterListNode : AstNode
{
    public List<FormalParameterNode> Parameters { get; }

    public FormalParameterListNode(List<FormalParameterNode> parameters, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Parameters = parameters;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFormalParameterListNode(this);
    }
}
