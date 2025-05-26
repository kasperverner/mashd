using Mashd.Backend.Adapters;
using Mashd.Frontend.AST;

namespace Mashd.Backend.Value;

public class DatasetValue : IValue
{
    public DatasetValue(SchemaValue schema, string? source, string? adapter, string? query, string? delimiter)
    {
        Schema = schema;
        Source = source;
        Adapter = adapter;
        Query = query;
        Delimiter = delimiter;
    }

    public DatasetValue(SchemaValue schema, List<Dictionary<string, object>> data)
    {
        Schema = schema;
        Data = data;
    }
    
    public readonly SchemaValue Schema;
    public readonly string? Source;
    public readonly string? Adapter;
    public readonly string? Query;
    public readonly string? Delimiter;

    public List<Dictionary<string, object>> Data { get; } = [];
    
    public override string ToString()
    {
        return $"Dataset: {{ Schema: {Schema.ToString()}, Source: {Source}, Adapter: {Adapter}, Query: {Query}, Delimiter: {Delimiter} }}";
    }
}