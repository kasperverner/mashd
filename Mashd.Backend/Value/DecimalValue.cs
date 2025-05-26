using System.Globalization;

namespace Mashd.Backend.Value;

public class DecimalValue(double raw) : IValue
{
    public double Raw = raw;

    public static DecimalValue TryParse(string? raw)
    {
        if (!double.TryParse(raw, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Cannot parse '{raw}' as a decimal.");
            
        }
        
        return new DecimalValue(result);
    }

    public override string ToString()
    {
        return Raw.ToString(CultureInfo.InvariantCulture);
    }
}