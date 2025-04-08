namespace Mashd.Frontend.AST.Expressions.Statements;

public class CompoundAssignmentStatement : StatementBase
{
    public string Identifier { get; set; }
    public string Operator { get; set; }
    public Expression Value { get; set; }
    
    public CompoundAssignmentStatement(string identifier, string operatorSymbol, Expression value)
    {
        Identifier = identifier;
        Operator = operatorSymbol;
        Value = value;
    }
}