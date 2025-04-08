namespace Mashd.Frontend.AST.Expressions.Terms.Schemas;

public class Schema : Expression
{
    public List<Column> Columns { get; set; } = new List<Column>();
    
    public Schema()
    {
    }
}