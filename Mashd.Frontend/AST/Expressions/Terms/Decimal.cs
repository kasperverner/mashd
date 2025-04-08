namespace Mashd.Frontend.AST.Expressions.Terms;

public class Decimal : Term
{
    public string Value { get; set; }
    
    public Decimal(string value)
    {
        Value = value;
    }
}