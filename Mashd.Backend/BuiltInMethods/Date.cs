using System.Globalization;

namespace Mashd.Backend.BuiltInMethods;

public static class Date
{
    public static DateTime Parse(string dateString, string format = null)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ArgumentException("The date string cannot be null or empty.", nameof(dateString));
        }

        if (string.IsNullOrEmpty(format))
        {
            // Default to ISO 8601 parsing
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
            {
                return result;
            }
        }
        else
        {
            // Use the provided format
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        throw new FormatException($"The date string '{dateString}' is not in a valid format.");
    }
}