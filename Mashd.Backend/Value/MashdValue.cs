using Mashd.Backend.Match;

namespace Mashd.Backend.Value;

public class MashdValue(string leftIdentifier, DatasetValue leftDataset, string rightIdentifier, DatasetValue rightDataset) : IValue
{
    public readonly Dictionary<string, DatasetValue> Datasets = new()
    {
        { leftIdentifier, leftDataset },
        { rightIdentifier, rightDataset }
    };
    
    public ObjectValue? OutputObject { get; private set; } = null;
    public SchemaValue? OutputSchema { get; private set; } = null;
    public Queue<IMatch> MatchRules { get; } = [];
    
    public override string ToString()
    {
        return leftDataset.ToString() + " & " + rightDataset.ToString();
    }

    public void AddMatch(PropertyAccessValue left, PropertyAccessValue right)
    {
        MatchRules.Enqueue(new SimpleMatch(left, right));
    }
    
    public void AddMatch(PropertyAccessValue left, PropertyAccessValue right, DecimalValue threshold)
    {
        MatchRules.Enqueue(new FuzzyMatch(left, right, threshold));
    }
    
    public void AddMatch(TextValue identifier, params object[] arguments)
    {
        MatchRules.Enqueue(new FunctionMatch(identifier, arguments));
    }
    
    public void Transform(ObjectValue schemaObject)
    {
        var properties = schemaObject.Raw
            .Where(entry => entry.Value is PropertyAccessValue)
            .ToDictionary(
                entry => entry.Key, 
                entry => ((PropertyAccessValue)entry.Value).FieldValue
            );
        OutputObject = schemaObject;
        OutputSchema = new SchemaValue(properties);
    }

    // public DatasetValue Join()
    // {
    //     
    // }
}