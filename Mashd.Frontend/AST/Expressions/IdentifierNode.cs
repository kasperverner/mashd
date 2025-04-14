namespace Mashd.Frontend.AST.Expressions;

public class IdentifierNode : ExpressionNode
{
    public string Name { get; }

    public IdentifierNode(string name, int line, int column, string text)
        : base(line, column, text)
    {
        Name = name;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIdentifierNode(this);
    }
}