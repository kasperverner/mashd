namespace Mashd.Frontend.AST.Expressions.Terms.Mashd;

public class Mashd : Expression
{
    public string Left { get; set; }
    public string Right { get; set; }

    public List<MatchStrategyBase> MatchStrategies { get; set; } = new List<MatchStrategyBase>();
}