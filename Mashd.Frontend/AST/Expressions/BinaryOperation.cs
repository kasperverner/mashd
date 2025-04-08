namespace Mashd.Frontend.AST.Expressions;

public class BinaryOperation : Expression
{
    public Expression Left { get; set; }
    public Expression Right { get; set; }
    public string Op { get; set; }
    
    public BinaryOperation(Expression left, string op, Expression right)
    {
        Left = left;
        Op = op;
        Right = right;
    }
}