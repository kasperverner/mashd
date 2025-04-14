namespace Mashd.Frontend.AST.Expressions
{
    public abstract class ExpressionNode : AstNode
    {
        protected ExpressionNode(int line, int column, string text)
            : base(line, column, text)
        {
        }
    }
}