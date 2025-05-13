using Mashd.Frontend.AST;

namespace Mashd.Backend;

public abstract class Value
{
}

public class TypeValue : Value
{
    public SymbolType Type { get; }
    public TypeValue(SymbolType type) => Type = type;
    public override string ToString() => $"<type:{Type}>";
}


public class IntegerValue : Value
{
    public long Raw;

    public IntegerValue(long raw)
    {
        this.Raw = raw;
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
    
    public override string ToString() => Raw.ToString("yyyy-MM-dd");
}

public class DatasetValue : Value
{
    public List<Dictionary<string, Value>> Raw;

    public DatasetValue(List<Dictionary<string, Value>> raw)
    {
        this.Raw = raw;
    }

    public override string ToString()
    {
        return $"Dataset with {Raw.Count} rows";
    }

    public void ToFile(string fileName)
    {
        throw new NotImplementedException();
    }
    
    public void ToTable()
    {
        throw new NotImplementedException();
    }
}

public class MashdValue : Value
{
    public Dictionary<string, Value> Raw;

    public MashdValue(Dictionary<string, Value> raw)
    {
        this.Raw = raw;
    }

    public override string ToString()
    {
        return $"Mashd with {Raw.Count} keys";
    }
}
