namespace Mashd.Frontend.AST.Expressions.Terms.Datasets;

public class Dataset : Expression
{
    public List<DatasetDefinition> Properties { get; set; } = new List<DatasetDefinition>();
    
    public Dataset()
    {
    }
}