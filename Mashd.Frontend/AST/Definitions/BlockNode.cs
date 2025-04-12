namespace Mashd.Frontend.AST.Definitions;

using Mashd.Frontend.AST.Statements;

public class BlockNode : AstNode
{
    public List<StatementNode> Statements { get; }
    
    public BlockNode(List<StatementNode> statements, int line, int column, string text)
        : base(line, column, text)
    {
        Statements = statements;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBlockNode(this);
    }
}