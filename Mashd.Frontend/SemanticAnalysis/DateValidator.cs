using System.Globalization;

namespace Mashd.Frontend.SemanticAnalysis;

public class DateValidator
{
    public static bool Validate(string value)
    {
        return DateTime.TryParseExact(value,new[]
            {
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "dd-MM-yyyy",
            "MM-dd-yyyy"
        }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _);
    }
}