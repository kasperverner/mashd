namespace Mashd.Frontend.AST.Definitions;

public class FunctionDefinitionNode : DefinitionNode
{
    public VarType ReturnType { get; }
    public string FunctionName { get; }
    
    public FormalParameterListNode ParameterList { get; }
    
    public BlockNode BlockNode { get; }
    
    public FunctionDefinitionNode(string functionName, VarType returnType, FormalParameterListNode parameterList, BlockNode blockNode, int line, int column, string text)
        : base(line, column, text)
    {
        ReturnType = returnType;
        FunctionName = functionName;
        ParameterList = parameterList;
        BlockNode = blockNode;
    }
    
    public override T Accept<T>(IAstVisitor<T> visitor)
    {
        return visitor.VisitFunctionDefinitionNode(this);
    }
}