namespace Mashd.Frontend.AST.Expressions.Statements;

public class AssignmentStatement : StatementBase
{
    public string Identifier { get; set; }
    public Expression Value { get; set; }
    
    public AssignmentStatement(string identifier, Expression value)
    {
        Identifier = identifier;
        Value = value;
    }
}