namespace Mashd.Backend.Value;

public class IntegerValue(long raw) : IValue
{
    public long Raw = raw;

    public static IntegerValue TryParse(string? raw)
    {
        if (!long.TryParse(raw, out var result))
        {
            throw new ArgumentException($"Cannot parse '{raw}' as an integer.");
        }
        
        return new IntegerValue(result);
    }

    public override string ToString()
    {
        return Raw.ToString();
    }
}