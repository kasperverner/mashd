using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Definitions;

public class FormalParameterNode : DefinitionNode, IDeclaration
{
    public SymbolType DeclaredType { get; }
    public string Identifier { get; }

    public FormalParameterNode(SymbolType paramType, string identifier, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        DeclaredType = paramType;
        Identifier = identifier;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFormalParameterNode(this);
    }
}
