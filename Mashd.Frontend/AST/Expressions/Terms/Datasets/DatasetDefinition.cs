namespace Mashd.Frontend.AST.Expressions.Terms.Datasets;

public class DatasetDefinition
{
    public string Adapter { get; set; }
    public string Schema { get; set; }
    public string Source { get; set; }
    public string? Delimiter { get; set; }
    public string? Query { get; set; }

    public DatasetDefinition(string adapter, string schema, string source, string? delimiter, string? query)
    {
        Adapter = adapter;
        Schema = schema;
        Source = source;
        Delimiter = delimiter;
        Query = query;
    }
}