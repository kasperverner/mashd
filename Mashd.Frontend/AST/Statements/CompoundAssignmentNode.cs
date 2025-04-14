namespace Mashd.Frontend.AST.Statements;

using Mashd.Frontend.AST.Expressions;
public class CompoundAssignmentNode : StatementNode
{
    public string Identifier { get; }
    
    public OpType OperatorType { get; }
    public ExpressionNode Expression { get; }

    public CompoundAssignmentNode(string identifier, OpType op, ExpressionNode expression, int line, int column, string text)
        : base(line, column, text)
    {
        Identifier = identifier;
        OperatorType = op;
        Expression = expression;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitCompoundAssignmentNode(this);
    }
}