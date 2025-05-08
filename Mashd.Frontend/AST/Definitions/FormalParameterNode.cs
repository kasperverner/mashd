using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST;

public class FormalParameterNode : DefinitionNode, IDeclaration
{
    public SymbolType DeclaredType { get; }
    public string Identifier { get; }

    public FormalParameterNode(SymbolType paramType, string identifier, int line, int column, string text)
        : base(line, column, text)
    {
        DeclaredType = paramType;
        Identifier = identifier;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFormalParameterNode(this);
    }
}
