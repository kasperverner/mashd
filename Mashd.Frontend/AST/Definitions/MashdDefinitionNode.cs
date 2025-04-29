using Mashd.Frontend.AST.Expressions;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Definitions;

public class MashdDefinitionNode : DefinitionNode, IDeclaration
{
    public string Identifier { get; }
    public SymbolType DeclaredType { get; }
    public ExpressionNode Left { get; }
    public ExpressionNode Right { get; }

    public MashdDefinitionNode(string identifier, ExpressionNode left, ExpressionNode right, int line, int column, string text)
        : base(line, column, text)
    {
        Identifier = identifier;
        DeclaredType = SymbolType.Mashd;
        Left = left;
        Right = right;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitMashdDefinitionNode(this);
    }
}