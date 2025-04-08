namespace Mashd.Frontend.AST.Expressions.Terms.Schemas;

public class Column : Expression
{
    public string Key { get; set; }
    public List<ColumnDefinition> Properties { get; } = new List<ColumnDefinition>();
    
    public Column(string key)
    {
        Key = key;
    }
}