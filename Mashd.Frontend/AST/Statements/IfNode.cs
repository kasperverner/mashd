namespace Mashd.Frontend.AST.Statements;

using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.AST.Definitions;
public class IfNode : StatementNode
{
    public ExpressionNode Condition { get; }
    public BlockNode ThenBlock { get; }
    public BlockNode ElseBlock { get; }
    public bool HasElse { get; }
    
    public IfNode(ExpressionNode condition, BlockNode thenBlock, BlockNode elseBlock, bool hasElse, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Condition = condition;
        ThenBlock = thenBlock;
        ElseBlock = elseBlock;
        HasElse = hasElse;
    }
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIfNode(this);
    }
}