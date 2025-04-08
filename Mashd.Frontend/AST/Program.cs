using Mashd.Frontend.AST.Expressions;

namespace Mashd.Frontend.AST;

public class Program : AstNode
{
    public Expression Expression { get; set; }
    
    public Program(Expression expression)
    {
        Expression = expression;
    }
}