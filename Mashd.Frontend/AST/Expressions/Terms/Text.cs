namespace Mashd.Frontend.AST.Expressions.Terms;

public class Text : Term
{
    public string Value { get; set; }
    
    public Text(string value)
    {
        Value = value;
    }
}