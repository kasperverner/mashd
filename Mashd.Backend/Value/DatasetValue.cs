using Mashd.Backend.Adapters;
using Mashd.Frontend.AST;

namespace Mashd.Backend.Value;

public class DatasetValue(SchemaValue schema, string source, string adapter, string? query, string? delimiter) : IValue
{
    public readonly SchemaValue Schema = schema;
    public readonly string Source = source;
    public readonly string Adapter = adapter;
    public readonly string? Query = query;
    public readonly string? Delimiter = delimiter;

    public List<Dictionary<string, object>> Data { get; } = [];

    private Dictionary<string, object>? FirstRow => Data
        .FirstOrDefault()?
        .ToDictionary(
            x => x.Key, 
            x => x.Value, 
            StringComparer.OrdinalIgnoreCase
        );

    private void AddData(IEnumerable<Dictionary<string, object>> data)
    {
        if (Data.Count > 0)
            Data.Clear();
        
        Data.AddRange(data);
    }
    
    public void ValidateProperties()
    {
        if (Adapter is "sqlserver" or "postgresql" && string.IsNullOrWhiteSpace(Query))
        {
            throw new Exception($"Dataset {Source} missing 'query' property.");
        }

        if (Schema.Raw.Count == 0)
        {
            throw new Exception($"Dataset {Source} missing 'schema' property.");
        }
    }
    
    public void LoadData()
    {
        try
        {
            var adapter = AdapterFactory.CreateAdapter(Adapter, new Dictionary<string, string>
            {
                { "source", Source },
                { "query", Query ?? "" },
                { "delimiter", Delimiter ?? "," }
            });

            var data = adapter.ReadAsync().Result;
            AddData(data);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message, e);
        }
    }
    
    public void ValidateData()
    {
        if (FirstRow == null)
            return;

        var typeParsers = new Dictionary<SymbolType, Func<string?, IValue>>
        {
            { SymbolType.Integer, IntegerValue.TryParse },
            { SymbolType.Decimal, DecimalValue.TryParse },
            { SymbolType.Text, TextValue.TryParse },
            { SymbolType.Boolean, BooleanValue.TryParse },
            { SymbolType.Date, DateValue.TryParse }
        };
        
        foreach (var field in Schema.Raw)
        {
            var fieldName = field.Value.Name;
            
            if (!FirstRow.TryGetValue(fieldName, out var value))
                throw new Exception("Dataset has field '" + fieldName + "' that is not present in the data.");

            try
            {
                if (typeParsers.TryGetValue(field.Value.Type, out var parser))
                {
                    parser(value?.ToString());
                }
                else
                {
                    throw new Exception($"Unsupported SymbolType: {field.Value.Type}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Dataset has field '{field.Key}' with wrong data type.", e);
            }
        }
    }
    
    public override string ToString()
    {
        return $"Dataset: {{ Schema: {Schema.ToString()}, Source: {Source}, Adapter: {Adapter}, Query: {Query}, Delimiter: {Delimiter} }}";
    }

    public void ToFile(string path)
    {
        throw new NotImplementedException();
    }

    public void ToTable()
    {
        throw new NotImplementedException();
    }
}