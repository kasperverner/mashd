using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Definitions;

public class SchemaDefinitionNode : DefinitionNode, IDeclaration
{
    public string Identifier { get; }
    public SymbolType DeclaredType { get; }
    public SchemaObjectNode? ObjectNode { get; }

    public SchemaDefinitionNode(string identifier, SchemaObjectNode objectNode, int line, int column, string text) : base(line, column, text)
    {
        Identifier = identifier;
        DeclaredType = SymbolType.Schema;
        ObjectNode = objectNode;
    }

    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitSchemaDefinitionNode(this);
    }
}