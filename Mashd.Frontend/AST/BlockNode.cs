using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.AST.Statements;

namespace Mashd.Frontend.AST;


public class BlockNode : ScopeNode
{
    public List<StatementNode> Statements { get; }
    
    public BlockNode(List<StatementNode> statements, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Statements = statements;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitBlockNode(this);
    }
}