using Mashd.Frontend.AST;

namespace Mashd.Backend;

public abstract class Value
{
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
        if (double.TryParse(raw, out var result))
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
        return Raw.ToString();
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

public class TypeValue(SymbolType raw) : Value
{
    public readonly SymbolType Raw = raw;

    public override string ToString()
    {
        return Raw.ToString();
    }
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
    public readonly SchemaValue Schema = schema;
    public readonly string Source = source;
    public readonly string Adapter = adapter;
    public readonly string? Query = query;
    public readonly string? Delimiter = delimiter;

    public List<Dictionary<string, object>> Data { get; } = [];

    public void AddData(IEnumerable<Dictionary<string, object>> data)
    {
        Data.AddRange(data);
    }
    
    public override string ToString()
    {
        return $"Dataset: {{ Schema: {Schema.ToString()}, Source: {Source}, Adapter: {Adapter}, Query: {Query}, Delimiter: {Delimiter} }}";
    }
}

public class MashdValue(Value left, Value right) : Value
{
    public readonly Value Left = left;
    public readonly Value Right = right;

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
