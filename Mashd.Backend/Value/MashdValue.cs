using Mashd.Backend.Match;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Backend.Value;

public class MashdValue(string leftIdentifier, DatasetValue leftDataset, string rightIdentifier, DatasetValue rightDataset) : IValue
{
    public string LeftIdentifier { get; } = leftIdentifier;
    public DatasetValue LeftDataset { get; } = leftDataset;
    public string RightIdentifier { get; } = rightIdentifier;
    public DatasetValue RightDataset { get; } = rightDataset;
    public ObjectExpressionNode? Transform { get; private set; } = null;
    public List<ICondition> Conditions { get; } = [];
    
    public override string ToString()
    {
        return LeftDataset.ToString() + " & " + RightDataset.ToString();
    }

    public void AddCondition(PropertyAccessValue left, PropertyAccessValue right)
    {
        Conditions.Add(new MatchCondition(left, right));
    }
    
    public void AddCondition(PropertyAccessValue left, PropertyAccessValue right, DecimalValue threshold)
    {
        Conditions.Add(new FuzzyMatchCondition(left, right, threshold));
    }
    
    public void AddCondition(FunctionDefinitionValue function)
    {
        Conditions.Add(new FunctionMatchCondition(function, LeftIdentifier, RightIdentifier));
    }
    
    public void SetTransform(ObjectExpressionNode schemaObject)
    {
        Transform = schemaObject;
    }
}