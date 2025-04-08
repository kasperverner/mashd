namespace Mashd.Frontend.AST.Expressions.Terms;

public class Boolean : Term
{
    public string Value { get; set; }
    
    public Boolean(string value)
    {
        Value = value;
    }
}