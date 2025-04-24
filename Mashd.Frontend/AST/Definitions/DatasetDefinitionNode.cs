using Mashd.Frontend.TypeChecking;

namespace Mashd.Frontend.AST.Definitions;

public class DatasetDefinitionNode : DefinitionNode, IDeclaration
{
    public string Identifier { get; }
    public SymbolType DeclaredType { get; }
    public DatasetObjectNode ObjectNode { get; }
    
    public DatasetDefinitionNode(string identifier, DatasetObjectNode objectNode, int line, int column, string text) : base(line, column, text)
    {
        Identifier = identifier;
        DeclaredType = SymbolType.Dataset;
        ObjectNode = objectNode;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitDatasetDefinitionNode(this);
    }
}