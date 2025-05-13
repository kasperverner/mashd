using System.Globalization;

namespace Mashd.Backend.BuiltInMethods;

public class Date
{
    public DateTime Value { get; }

    private Date(DateTime value)
    {
        Value = value;
    }
    public static Date parse(string dateString, string format = null)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ArgumentException("The date string cannot be null or empty.", nameof(dateString));
        }

        // Time-only format special case
        bool isTimeOnlyFormat = !string.IsNullOrEmpty(format) && 
                                !format.Contains("y") && !format.Contains("M") && !format.Contains("d");

        try
        {
            DateTime result;

            if (string.IsNullOrEmpty(format))
            {
                // Default parsing (handles ISO 8601)
                result = DateTime.Parse(dateString, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            }
            else
            {
                // Parse with specific format
                result = DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                // For time-only formats, use a base date of 0001-01-01
                if (isTimeOnlyFormat)
                {
                    result = new DateTime(1, 1, 1, result.Hour, result.Minute, result.Second,
                        result.Millisecond, DateTimeKind.Utc);
                }
            }

            return new Date(result);
        }
        catch (FormatException)
        {
            throw new FormatException($"The date string '{dateString}' is not in a valid format.");
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"The date string '{dateString}' is not valid.");
        }
    }
    
}