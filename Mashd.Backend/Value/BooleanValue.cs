namespace Mashd.Backend.Value;

public class BooleanValue(bool raw) : IValue
{
    public bool Raw = raw;

    public static BooleanValue TryParse(string? raw)
    {
        if (!bool.TryParse(raw, out var result))
            throw new ArgumentException($"Cannot parse '{raw}' as a boolean.");

        return new BooleanValue(result);
    }

    public override string ToString()
    {
        return Raw.ToString();
    }
}