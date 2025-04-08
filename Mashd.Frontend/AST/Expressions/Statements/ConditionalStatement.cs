namespace Mashd.Frontend.AST.Expressions.Statements;

public class ConditionalStatement : StatementBase
{
    public Expression Condition { get; set; }
    public StatementBase TrueBlock { get; set; }
    public StatementBase? FalseBlock { get; set; }
    
    public ConditionalStatement(Expression condition, StatementBase trueBlock, StatementBase falseBlock)
    {
        Condition = condition;
        TrueBlock = trueBlock;
        FalseBlock = falseBlock;
    }
}