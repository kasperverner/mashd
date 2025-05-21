using Mashd.Backend.Match;

namespace Mashd.Backend.Value;

public class MashdValue(DatasetValue left, DatasetValue right) : IValue
{
    public readonly DatasetValue Left = left;
    public readonly DatasetValue Right = right;
    public List<IMatch> MatchRules { get; } = [];
    
    public override string ToString()
    {
        return Left.ToString() + " & " + Right.ToString();
    }

    public void AddMatch(SchemaFieldValue left, SchemaFieldValue right)
    {
        MatchRules.Add(new Match.Match(left, right));
    }
    
    public void AddMatch(SchemaFieldValue left, SchemaFieldValue right, DecimalValue threshold)
    {
        MatchRules.Add(new Match.FuzzyMatch(left, right, threshold));
    }
}