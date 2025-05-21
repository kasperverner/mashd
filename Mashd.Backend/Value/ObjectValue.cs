namespace Mashd.Backend.Value;

public class ObjectValue(Dictionary<string, IValue> raw) : IValue
{
    public readonly Dictionary<string, IValue> Raw = raw;

    public DatasetValue ToDatasetValue()
    {
        var properties = new Dictionary<string, string>();
        foreach (var (key, val) in Raw)
            if (val is TextValue textValue)
                properties[key] = textValue.Raw;

        if (!properties.TryGetValue("source", out var source))
            throw new Exception($"Dataset missing 'source' property.");

        if (string.IsNullOrWhiteSpace(source))
            throw new Exception($"Dataset source is empty.");
        
        if (!properties.TryGetValue("adapter", out var adapter))
            throw new Exception($"Dataset missing 'adapter' property.");

        if (string.IsNullOrWhiteSpace(adapter))
            throw new Exception($"Dataset adapter is empty.");
        
        if (!Raw.TryGetValue("schema", out var schemaObject))
            throw new Exception($"Dataset missing 'schema' property.");

        var schema = schemaObject switch
        {
            ObjectValue schemaObjectValue => schemaObjectValue.ToSchemaValue(),
            _ => throw new Exception($"Dataset schema is not an object value. Got {schemaObject.GetType()}.")
        };

        var query = properties.GetValueOrDefault("query");
        var delimiter = properties.GetValueOrDefault("delimiter");

        return new DatasetValue(schema, source, adapter, query, delimiter);
    }
    
    public SchemaValue ToSchemaValue()
    {
        var fields = new Dictionary<string, SchemaFieldValue>();

        foreach (var (identifier, fieldValue) in Raw)
        {
            if (fieldValue is not ObjectValue fieldObjectValue) continue;

            fields[identifier] = fieldObjectValue.ToSchemaFieldValue();
        }

        return new SchemaValue(fields);
    }

    public SchemaFieldValue ToSchemaFieldValue()
    {
        var name = Raw.GetValueOrDefault("name");
        var type = Raw.GetValueOrDefault("type");

        if (name is not TextValue textValue || type is not TypeValue typeValue)
            throw new ArgumentException("Invalid object value for schema field.");
        
        return new SchemaFieldValue(typeValue.Raw, textValue.Raw);
    }
    
    public override string ToString()
    {
        return "{" + string.Join(", ", Raw.Select(x => $"{x.Key}: {x.Value}")) + "}";
    }
}