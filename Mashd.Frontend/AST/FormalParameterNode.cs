namespace Mashd.Frontend.AST;

public class FormalParameterNode : AstNode
{
    public VarType ParamType { get; }
    public string Identifier { get; }

    public FormalParameterNode(VarType paramType, string identifier, int line, int column, string text)
        : base(line, column, text)
    {
        ParamType = paramType;
        Identifier = identifier;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFormalParameterNode(this);
    }
}
