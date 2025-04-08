namespace Mashd.Frontend.AST.Expressions.Terms.Schemas;

public class ColumnDefinition : Expression
{
    public string Type { get; set; }
    public string Name { get; set; }
    
    public ColumnDefinition(string type, string name)
    {
        Type = type;
        Name = name;
    }
}