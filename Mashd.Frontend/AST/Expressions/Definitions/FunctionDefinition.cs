using Mashd.Frontend.AST.Expressions.Statements;

namespace Mashd.Frontend.AST.Expressions.Definitions;

public class FunctionDefinition : AstNode
{
    public string Type { get; set; }
    public string Identifier { get; set; }
    public List<ParameterNode> Parameters { get; set; } = new List<ParameterNode>();
    public StatementBase Body { get; set; }
    
    public FunctionDefinition(string type, string identifier, StatementBase body)
    {
        Type = type;
        Identifier = identifier;
        Body = body;
    }
}