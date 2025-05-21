namespace Mashd.Backend.Value;

public class TextValue(string raw) : IValue
{
    public string Raw = raw;

    public static TextValue TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException($"Cannot parse '{raw}' as text.");
        }
        
        return new TextValue(raw);
    }

    public override string ToString()
    {
        return Raw;
    }
}