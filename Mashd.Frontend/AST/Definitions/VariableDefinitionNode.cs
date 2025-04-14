namespace Mashd.Frontend.AST.Definitions;

public class VariableDefinitionNode : DefinitionNode
{
    public VarType VarType { get; }
    public string Identifier { get; }

    public VariableDefinitionNode(string identifier, VarType varType, int line, int column, string text)
        : base(line, column, text)
    {
        VarType = varType;
        Identifier = identifier;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitVariableDefinitionNode(this);
    }
}