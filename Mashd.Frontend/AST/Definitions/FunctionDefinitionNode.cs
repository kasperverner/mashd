using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Frontend.AST.Definitions;

public class FunctionDefinitionNode : DefinitionNode, IDeclaration
{
    public SymbolType DeclaredType { get; }
    public string Identifier { get; }
    
    public FormalParameterListNode ParameterList { get; }
    
    public BlockNode Body { get; }
    
    public FunctionDefinitionNode(string functionName, SymbolType returnType, FormalParameterListNode parameterList, BlockNode body, int line, int column, string text, int level)
        : base(line, column, text, level)
    {
        DeclaredType = returnType;
        Identifier = functionName;
        ParameterList = parameterList;
        Body = body;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFunctionDefinitionNode(this);
    }
}