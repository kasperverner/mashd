namespace Mashd.Frontend.AST.Expressions.Terms.Mashd;

public class FunctionMatchStrategy : MatchStrategyBase
{
    public string FunctionName { get; set; }
    public List<string> Parameters { get; set; } = new List<string>();

    public FunctionMatchStrategy(string functionName, List<string> parameters)
    {
        FunctionName = functionName;
        Parameters = parameters;
    }
}