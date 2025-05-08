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
