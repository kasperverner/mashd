namespace Mashd.Frontend.AST.Expressions;

public class DateLiteralNode : ExpressionNode
{
    public DateTime Value { get; }

    public DateLiteralNode(DateTime value, int line, int column, string text)
        : base(line, column, text)
    {
        Value = value;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitDateLiteralNode(this);
    }
}