using Mashd.Frontend.AST.Expressions.Definitions;

namespace Mashd.Frontend.AST.Expressions.Statements;

public class BlokStatement : StatementBase
{
    public List<VariableDefinition> ScopedVariables { get; set; } = new List<VariableDefinition>();
    public StatementBase Statement { get; set; }
    
    public BlokStatement(StatementBase statement)
    {
        Statement = statement;
    }
}