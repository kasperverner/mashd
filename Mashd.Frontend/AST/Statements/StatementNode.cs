namespace Mashd.Frontend.AST.Statements;

public abstract class StatementNode : AstNode
{
    protected StatementNode(int line, int column, string text)
        : base(line, column, text)
    {
    }

}