namespace Mashd.Frontend.AST.Expressions.Statements;

public class ReturnStatement : StatementBase
{
    public Expression Value { get; set; }
    
    public ReturnStatement(Expression value)
    {
        Value = value;
    }
}