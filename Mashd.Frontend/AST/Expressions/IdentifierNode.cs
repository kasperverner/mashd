using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Expressions;

public class IdentifierNode : ExpressionNode
{
    public string Name { get; }
    public IDeclaration Definition {get; set; }

    public IdentifierNode(string name, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        Name = name;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitIdentifierNode(this);
    }
}