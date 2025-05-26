using Mashd.Backend.BuiltInMethods;

namespace Mashd.Test.Unit.Parsing;

public class DateTests
{
    [Theory]
    [InlineData("2023-10-01", 2023, 10, 1)]
    [InlineData("2000-01-01", 2000, 1, 1)]
    [InlineData("1999-12-31", 1999, 12, 31)]
    public void DateValue_CanParseValidDate(string dateString, int year, int month, int day)
    {
        // Arrange
        var expectedDate = new DateTime(year, month, day);
        
        // Act
        var dateValue = Date.parse(dateString);
        
        // Assert
        Assert.Equal(expectedDate, dateValue.Value);
    }
    
    [Fact]
    public void DateValue_CanParseInvalidDate()
    {
        // Arrange
        string invalidDateString = "2023-13-01"; // Invalid month

        // Act & Assert
        Assert.Throws<FormatException>(() => Date.parse(invalidDateString));
    }
    
    [Theory]
    [InlineData("2023-10-01", null, 2023, 10, 1)] // ISO 8601 format
    [InlineData("01-10-2023", "dd-MM-yyyy", 2023, 10, 1)] // Custom format
    [InlineData("10/01/2023", "MM/dd/yyyy", 2023, 10, 1)] // Custom format
    public void DateParse_ValidDates_ShouldReturnCorrectDate(string dateString, string format, int year, int month, int day)
    {
        // Arrange
        var expectedDate = new DateTime(year, month, day);

        // Act
        var parsedDate = Date.parse(dateString, format);

        // Assert
        Assert.Equal(expectedDate, parsedDate.Value);
    }

    
    [Theory]
    [InlineData("2023-13-01", null)] // Invalid ISO 8601 date
    [InlineData("01-32-2023", "MM-dd-yyyy")] // Invalid custom format
    [InlineData("invalid-date", null)] // Completely invalid date
    public void DateParse_InvalidDates_ShouldThrowFormatException(string dateString, string format)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => Date.parse(dateString, format));
    }

    [Fact]
    public void DateParse_NullOrEmptyInput_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Date.parse(null));
        Assert.Throws<ArgumentException>(() => Date.parse(""));
    }

    [Fact]
    public void DateParse_ShouldHandleISO8601WithTimeAndZone()
    {
        // Arrange
        string input = "2020-07-10T15:00:00.000Z";
        var expectedDate = DateTime.Parse("2020-07-10T15:00:00.000Z").ToUniversalTime();

        // Act
        var parsedDate = Date.parse(input);

        // Assert
        Assert.Equal(expectedDate, parsedDate.Value);
    }

    [Theory]
    [InlineData("2023-10-01T15:30:00", null, 2023, 10, 1, 15, 30, 0)] // ISO 8601 with time
    [InlineData("2023-10-01T15:30:00.123", null, 2023, 10, 1, 15, 30, 0, 123)] // ISO 8601 with milliseconds
    [InlineData("2023-10-01T15:30:00Z", null, 2023, 10, 1, 15, 30, 0)] // ISO 8601 UTC
    [InlineData("2023-10-01T15:30:00+02:00", null, 2023, 10, 1, 13, 30, 0)] // ISO 8601 with offset
    [InlineData("15:30:00", "HH:mm:ss", 1, 1, 1, 15, 30, 0)] // Custom time-only format
    public void DateParse_ValidTimeFormats_ShouldReturnCorrectDate(
        string dateString,
        string format,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond = 0)
    {
        // Arrange
        var expectedDate = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);

        // Act
        var parsedDate = Date.parse(dateString, format);

        // Assert
        Assert.Equal(expectedDate, parsedDate.Value);
    }

    [Theory]
    [InlineData("25:30:00", "HH:mm:ss")]
    [InlineData("15:61:00", "HH:mm:ss")]
    [InlineData("15:30:61", "HH:mm:ss")]
    [InlineData("invalid-time", "HH:mm:ss")]
    public void DateParse_InvalidTimeFormats_ShouldThrowFormatException(string dateString, string format)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => Date.parse(dateString, format));
    }
   
}

