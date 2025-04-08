namespace Mashd.Frontend.AST.Expressions;

public class UnaryOperation : Expression
{
    public Expression Expression { get; set; }
    public string Op { get; set; }
    
    public UnaryOperation(string op, Expression expression)
    {
        Op = op;
        Expression = expression;
    }
}