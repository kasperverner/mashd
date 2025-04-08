namespace Mashd.Frontend.AST.Expressions.Terms.Mashd;

public class MatchStrategy : MatchStrategyBase
{
    public string Left { get; set; }
    public string Right { get; set; }

    public MatchStrategy(string left, string right)
    {
        Left = left;
        Right = right;
    }
}