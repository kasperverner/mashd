namespace Mashd.Backend.Value;

public class DateValue(DateTime raw) : IValue
{
    public DateTime Raw { get; } = raw;

    public static DateValue TryParse(string? raw)
    {
        if (!DateTime.TryParse(raw, out var result))
        {
            throw new ArgumentException($"Cannot parse '{raw}' as a date.");
        }
        
        return new DateValue(result);
    }

    public override string ToString() => Raw.ToString("yyyy-MM-dd");
}