namespace Mashd.Frontend.AST.Expressions.Statements;

public class SequenceStatement : StatementBase
{
    public List<StatementBase> Statements { get; set; } = new List<StatementBase>();
    
    public SequenceStatement(List<StatementBase> statements)
    {
        Statements = statements;
    }
}