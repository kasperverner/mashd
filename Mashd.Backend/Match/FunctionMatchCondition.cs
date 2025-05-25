using Mashd.Backend.Value;

namespace Mashd.Backend.Match;

public class FunctionMatchCondition(FunctionDefinitionValue function, string leftIdentifier, string rightIdentifier) : ICondition
{
    public FunctionDefinitionValue Function { get; } = function;
    public string LeftIdentifier { get; } = leftIdentifier;
    public string RightIdentifier { get; } = rightIdentifier;
}