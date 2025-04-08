namespace Mashd.Frontend.AST.Expressions.Terms.Mashd;

public class FuzzyMatchStrategy : MatchStrategyBase
{
    public string Left { get; set; }
    public string Right { get; set; }
    public string Threshold { get; set; }
    
    public FuzzyMatchStrategy(string left, string right, string threshold)
    {
        Left = left;
        Right = right;
        Threshold = threshold;
    }
}