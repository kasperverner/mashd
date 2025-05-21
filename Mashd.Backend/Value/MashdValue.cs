using Mashd.Backend.Match;

namespace Mashd.Backend.Value;

public class MashdValue(DatasetValue left, DatasetValue right) : IValue
{
    public readonly DatasetValue Left = left;
    public readonly DatasetValue Right = right;
    public Queue<IMatch> MatchRules { get; } = [];
    
    public override string ToString()
    {
        return Left.ToString() + " & " + Right.ToString();
    }

    public void AddMatch(TextValue left, TextValue right)
    {
        MatchRules.Enqueue(new SimpleMatch(left, right));
    }
    
    public void AddMatch(TextValue left, TextValue right, DecimalValue threshold)
    {
        MatchRules.Enqueue(new FuzzyMatch(left, right, threshold));
    }
    
    public void AddMatch(TextValue identifier, params object[] arguments)
    {
        MatchRules.Enqueue(new FunctionMatch(identifier, arguments));
    }
}