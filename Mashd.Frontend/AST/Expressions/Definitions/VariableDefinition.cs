namespace Mashd.Frontend.AST.Expressions.Definitions;

public class VariableDefinition : AstNode
{
    public string Type { get; set; }
    public string Identifier { get; set; }
    public Expression? Value { get; set; }
    
    public VariableDefinition(string type, string identifier, Expression? value = null)
    {
        Type = type;
        Identifier = identifier;
        Value = value;
    }
}