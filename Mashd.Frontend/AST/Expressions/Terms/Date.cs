namespace Mashd.Frontend.AST.Expressions.Terms;

public class Date : Term
{
    public string Value { get; set; }
    
    public Date(string value)
    {
        Value = value;
    }
}