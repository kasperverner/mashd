using Mashd.Frontend.AST.Definitions;

namespace Mashd.Backend.Value;

public class FunctionDefinitionValue(FunctionDefinitionNode node) : IValue
{
    public FunctionDefinitionNode Node { get; } = node;
    public List<IValue> Arguments { get; } = new List<IValue>();
    
    public void AddArgument(params IValue[] args)
        => Arguments.AddRange(args);
    
    public bool HasArguments => Arguments.Count > 0;
    
    public override string ToString()
    {
        return $"FunctionDefinition: {Node.Identifier}";
    }
}