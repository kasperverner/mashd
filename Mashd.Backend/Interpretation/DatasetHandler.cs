using Mashd.Backend.Adapters;
using Mashd.Backend.Value;
using Mashd.Frontend.AST;
using Mashd.Frontend.AST.Expressions;

namespace Mashd.Backend.Interpretation;

public class DatasetHandler
{
    public DatasetValue HandleDatasetFromObjectNode(IAstVisitor<IValue> visitor, ObjectExpressionNode node)
    {
        var value = node.Accept(visitor);

        if (value is not ObjectValue objectValue)
            throw new ParseException("Invalid dataset object value.", node.Line, node.Column);

        var dataset = BuildDatasetFromObject(objectValue);
        
        ValidateDatasetProperties(dataset);
        LoadDatasetData(dataset);
        ValidateDatasetData(dataset);

        return dataset;
    }

    public DatasetValue HandleDatasetFromMethodNode(IAstVisitor<IValue> visitor, MethodChainExpressionNode node)
    {
        var value = node.Accept(visitor);

        if (value is not DatasetValue datasetValue)
            throw new ParseException("Invalid dataset method chain value.", node.Line, node.Column);

        return datasetValue;
    }

    private DatasetValue BuildDatasetFromObject(ObjectValue value)
    {
        var properties = new Dictionary<string, string>();
        foreach (var (key, val) in value.Raw)
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
        
        if (!value.Raw.TryGetValue("schema", out var schemaObject))
            throw new Exception($"Dataset missing 'schema' property.");

        var schema = schemaObject switch
        {
            ObjectValue schemaObjectValue => BuildSchemaFromObject(schemaObjectValue),
            _ => throw new Exception($"Dataset schema is not an object value. Got {schemaObject.GetType()}.")
        };

        var query = properties.GetValueOrDefault("query");
        var delimiter = properties.GetValueOrDefault("delimiter");

        return new DatasetValue(schema, source, adapter, query, delimiter);
    }

    private SchemaValue BuildSchemaFromObject(ObjectValue value)
    {
        var fields = new Dictionary<string, SchemaFieldValue>();

        foreach (var (identifier, fieldValue) in value.Raw)
        {
            if (fieldValue is not ObjectValue fieldObjectValue) continue;

            fields[identifier] = BuildSchemaFieldValueFromObject(fieldObjectValue);
        }

        return new SchemaValue(fields);
    }

    private SchemaFieldValue BuildSchemaFieldValueFromObject(ObjectValue value)
    {
        var name = value.Raw.GetValueOrDefault("name");
        var type = value.Raw.GetValueOrDefault("type");

        if (name is not TextValue textValue || type is not TypeValue typeValue)
            throw new ArgumentException("Invalid object value for schema field.");
        
        return new SchemaFieldValue(typeValue.Raw, textValue.Raw);
    }
    
    private void ValidateDatasetProperties(DatasetValue dataset)
    {
        if (dataset.Adapter is "sqlserver" or "postgresql" && string.IsNullOrWhiteSpace(dataset.Query))
        {
            throw new Exception($"Dataset {dataset.Source} missing 'query' property.");
        }

        if (dataset.Schema.Raw.Count == 0)
        {
            throw new Exception($"Dataset {dataset.Source} missing 'schema' property.");
        }
    }

    private void LoadDatasetData(DatasetValue dataset)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataset.Adapter))
                throw new Exception($"Dataset {dataset.Source} missing 'adapter' property.");
            
            if (string.IsNullOrWhiteSpace(dataset.Source))
                throw new Exception($"Dataset {dataset.Source} missing 'source' property.");
            
            var adapter = AdapterFactory.CreateAdapter(dataset.Adapter, new Dictionary<string, string>
            {
                { "source", dataset.Source },
                { "query", dataset.Query ?? "" },
                { "delimiter", dataset.Delimiter ?? "," }
            });

            var data = adapter.ReadAsync().Result;
            if (dataset.Data.Count > 0)
                dataset.Data.Clear();
        
            dataset.Data.AddRange(data);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message, e);
        }
    }

    private void ValidateDatasetData(DatasetValue dataset)
    {
        var firstRow = dataset.Data
            .FirstOrDefault()?
            .ToDictionary(
                x => x.Key, 
                x => x.Value, 
                StringComparer.OrdinalIgnoreCase
            );
        
        if (firstRow == null)
            return;

        var typeParsers = new Dictionary<SymbolType, Func<string?, IValue>>
        {
            { SymbolType.Integer, IntegerValue.TryParse },
            { SymbolType.Decimal, DecimalValue.TryParse },
            { SymbolType.Text, TextValue.TryParse },
            { SymbolType.Boolean, BooleanValue.TryParse },
            { SymbolType.Date, DateValue.TryParse }
        };
        
        foreach (var field in dataset.Schema.Raw)
        {
            var fieldName = field.Value.Name;
            
            if (!firstRow.TryGetValue(fieldName, out var value))
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
}