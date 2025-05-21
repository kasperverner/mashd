using System.Globalization;
using Mashd.Backend.Adapters;
using Mashd.Frontend.AST;

namespace Mashd.Backend;

public abstract class Value
{
}

public class TypeValue : Value
{
    public SymbolType Raw { get; }
    public TypeValue(SymbolType raw) => Raw = raw;
    public override string ToString() => $"<type:{Raw}>";
}


public class IntegerValue : Value
{
    public long Raw;

    public IntegerValue(long raw)
    {
        this.Raw = raw;
    }

    public static IntegerValue TryParse(string? raw)
    {
        if (long.TryParse(raw, out var result))
        {
            return new IntegerValue(result);
        }
        else
        {
            throw new ArgumentException($"Cannot parse '{raw}' as an integer.");
        }
    }

    public override string ToString()
    {
        return Raw.ToString();
    }
}

public class DecimalValue : Value
{
    public double Raw;

    public DecimalValue(double raw)
    {
        this.Raw = raw;
    }

    public static DecimalValue TryParse(string? raw)
    {
        if (double.TryParse(raw, CultureInfo.InvariantCulture, out var result))
        {
            return new DecimalValue(result);
        }
        else
        {
            throw new ArgumentException($"Cannot parse '{raw}' as a decimal.");
        }
    }

    public override string ToString()
    {
        return Raw.ToString(CultureInfo.InvariantCulture);
    }
}

public class TextValue : Value
{
    public string Raw;

    public TextValue(string raw)
    {
        this.Raw = raw;
    }

    public static TextValue TryParse(string? raw)
    {
        if (raw != null)
        {
            return new TextValue(raw);
        }
        else
        {
            throw new ArgumentException($"Cannot parse '{raw}' as text.");
        }
    }

    public override string ToString()
    {
        return Raw;
    }
}

public class BooleanValue : Value
{
    public bool Raw;

    public BooleanValue(bool raw)
    {
        this.Raw = raw;
    }

    public static BooleanValue TryParse(string? raw)
    {
        if (bool.TryParse(raw, out var result))
        {
            return new BooleanValue(result);
        }
        else
        {
            throw new ArgumentException($"Cannot parse '{raw}' as a boolean.");
        }
    }

    public override string ToString()
    {
        return Raw.ToString();
    }
}

public class DateValue : Value
{
    public DateTime Raw { get; }

    public DateValue(DateTime raw)
    {
        Raw = raw;
    }

    public static DateValue TryParse(string? raw)
    {
        if (DateTime.TryParse(raw, out var result))
        {
            return new DateValue(result);
        }
        else
        {
            throw new ArgumentException($"Cannot parse '{raw}' as a date.");
        }
    }

    public override string ToString() => Raw.ToString("yyyy-MM-dd");
}

public class ObjectValue(Dictionary<string, Value> raw) : Value
{
    public readonly Dictionary<string, Value> Raw = raw;

    public override string ToString()
    {
        return "{" + string.Join(", ", Raw.Select(kvp => $"{kvp.Key}: {kvp.Value.ToString()}")) + "}";
    }
}

public class SchemaValue(Dictionary<string, SchemaFieldValue> raw) : Value
{
    public readonly Dictionary<string, SchemaFieldValue> Raw = raw;

    public override string ToString()
    {
        return "{" + string.Join(", ", Raw.Select(kvp => $"{kvp.Key}: {kvp.Value.ToString()}")) + "}";
    }
}

public class SchemaFieldValue(SymbolType type, string name) : Value
{
    public readonly SymbolType Type = type;
    public readonly string Name = name;

    public override string ToString()
    {
        return $"{{ Type: {Type.ToString()}, Name: {Name} }}";
    }
}

public class DatasetValue(SchemaValue schema, string source, string adapter, string? query, string? delimiter) : Value
{
    private readonly SchemaValue _schema = schema;
    private readonly string _source = source;
    private readonly string _adapter = adapter;
    private readonly string? _query = query;
    private readonly string? _delimiter = delimiter;

    public List<Dictionary<string, object>> Data { get; } = [];

    private void AddData(IEnumerable<Dictionary<string, object>> data)
    {
        if (Data.Count > 0)
            Data.Clear();
        
        Data.AddRange(data);
    }
    
    public void ValidateProperties()
    {
        if (_adapter is "sqlserver" or "postgresql" && string.IsNullOrWhiteSpace(_query))
        {
            throw new Exception($"Dataset {_source} missing 'query' property.");
        }

        if (_schema.Raw.Count == 0)
        {
            throw new Exception($"Dataset {_source} missing 'schema' property.");
        }
    }
    
    public void LoadData()
    {
        try
        {
            var adapter = AdapterFactory.CreateAdapter(_adapter, new Dictionary<string, string>
            {
                { "source", _source },
                { "query", _query ?? "" },
                { "delimiter", _delimiter ?? "," }
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
        var firstRow = Data.FirstOrDefault();

        if (firstRow == null)
            return;

        var comparer = StringComparer.OrdinalIgnoreCase;
        var row = firstRow.ToDictionary(
            x => x.Key, 
            x => x.Value, 
            comparer);
        
        var typeParsers = new Dictionary<SymbolType, Func<string?, Value>>
        {
            { SymbolType.Integer, IntegerValue.TryParse },
            { SymbolType.Decimal, DecimalValue.TryParse },
            { SymbolType.Text, TextValue.TryParse },
            { SymbolType.Boolean, BooleanValue.TryParse },
            { SymbolType.Date, DateValue.TryParse }
        };
        
        foreach (var field in _schema.Raw)
        {
            var fieldName = field.Value.Name;
            
            if (!row.TryGetValue(fieldName, out var value))
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
        return $"Dataset: {{ Schema: {_schema.ToString()}, Source: {_source}, Adapter: {_adapter}, Query: {_query}, Delimiter: {_delimiter} }}";
    }
}

public class MashdValue(DatasetValue left, DatasetValue right) : Value
{
    public readonly DatasetValue Left = left;
    public readonly DatasetValue Right = right;

    public override string ToString()
    {
        return Left.ToString() + " & " + Right.ToString();
    }
}

public class NullValue : Value
{
    public override string ToString()
    {
        return "null";
    }
}
