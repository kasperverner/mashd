using System.Diagnostics.CodeAnalysis;
using Mashd.Backend.Value;
using Mashd.Frontend.AST.Definitions;
using Mashd.Frontend.SemanticAnalysis;

namespace Mashd.Backend.Interpretation;

public class FunctionHandler
{
    private readonly Dictionary<FunctionDefinitionNode, FunctionDefinitionNode> _functions = new();
    
    public void Register(FunctionDefinitionNode function) => _functions[function] = function;
    
    public bool TryGetFunction(FunctionDefinitionNode key, [NotNullWhen(true)] out FunctionDefinitionNode? function)
    {
        return _functions.TryGetValue(key, out function);
    }
}