namespace Mashd.Frontend.AST.Expressions.Terms;

public class Integer : Term
{
    public string Value { get; set; }
    
    public Integer(string value)
    {
        Value = value;
    }
}