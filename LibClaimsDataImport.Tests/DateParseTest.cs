using LibClaimsDataImport.Importer;

namespace LibClaimsDataImport.Tests;

public class FileSpecTests
{
    [Theory]
    [InlineData("2023-12-25", 2023, 12, 25, 0, 0, 0)]
    [InlineData("12/25/2023", 2023, 12, 25, 0, 0, 0)]
    [InlineData("2023-12-25 14:30:00", 2023, 12, 25, 14, 30, 0)]
    [InlineData("Dec 25, 2023", 2023, 12, 25, 0, 0, 0)]
    [InlineData("2023/12/25", 2023, 12, 25, 0, 0, 0)]
    [InlineData("12-25-2023", 2023, 12, 25, 0, 0, 0)]
    [InlineData("2023-12-25T14:30:00", 2023, 12, 25, 14, 30, 0)]
    [InlineData("2023-12-25T14:30:45", 2023, 12, 25, 14, 30, 45)]
    [InlineData("1/1/2000", 2000, 1, 1, 0, 0, 0)]
    [InlineData("01/01/2000", 2000, 1, 1, 0, 0, 0)]
    [InlineData("01/03/2000", 2000, 1, 3, 0, 0, 0)]
    [InlineData("1/04/2000", 2000, 1, 4, 0, 0, 0)]
    [InlineData("1/04/00", 2000, 1, 4, 0, 0, 0)]
    [InlineData("3/9/21", 2021, 3, 9, 0, 0, 0)]
    [InlineData("04/9/25", 2025, 4, 9, 0, 0, 0)]
    [InlineData("9/1/25", 2025, 9, 1, 0, 0, 0)]
    [InlineData("6/01/24", 2024, 6, 1, 0, 0, 0)]
    [InlineData("6/3/24", 2024, 6, 3, 0, 0, 0)]
    [InlineData("2000-01-01", 2000, 1, 1, 0, 0, 0)]
    [InlineData("Jan 1, 2000", 2000, 1, 1, 0, 0, 0)]
    [InlineData("mar 1 24", 2024, 3, 1, 0, 0, 0)]
    [InlineData("AUG-19-25", 2025, 8, 19, 0, 0, 0)]
    public void TryParseDateTime_ValidDateFormats_ParsesCorrectly(string input, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        // Act
        bool result = DataTypeDetector.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.True(result, $"Failed to parse '{input}' as DateTime");
        Assert.Equal(expectedYear, parsedDate.Year);
        Assert.Equal(expectedMonth, parsedDate.Month);
        Assert.Equal(expectedDay, parsedDate.Day);
        Assert.Equal(expectedHour, parsedDate.Hour);
        Assert.Equal(expectedMinute, parsedDate.Minute);
        Assert.Equal(expectedSecond, parsedDate.Second);
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("123456")]
    [InlineData("abc-def-ghi")]
    [InlineData("25/13/2023")] // Invalid month
    [InlineData("32/12/2023")] // Invalid day
    [InlineData("2023-13-01")] // Invalid month
    [InlineData("2023-12-32")] // Invalid day
    [InlineData("25:30:70")] // Invalid time format
    [InlineData("13/25/2023")] // Invalid month when interpreted as MM/dd/yyyy
    [InlineData("2023-2-30")] // February 30th doesn't exist
    [InlineData("NotADate123")]
    [InlineData("2023-12-25-extra")]
    public void TryParseDateTime_InvalidDateFormats_ReturnsFalse(string? input)
    {
        // Act
        bool result = DataTypeDetector.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.False(result, $"Expected '{input}' to fail parsing but it succeeded with value: {parsedDate}");
        Assert.Equal(DateTime.MinValue, parsedDate);
    }

    [Theory]
    [InlineData("2023-12-25 15:30:45", 2023, 12, 25, 15, 30, 45)]
    [InlineData("2023-12-25T15:30:45", 2023, 12, 25, 15, 30, 45)]
    [InlineData("Dec 31, 1999 23:59:59", 1999, 12, 31, 23, 59, 59)]
    [InlineData("2024-02-29", 2024, 2, 29, 0, 0, 0)] // Leap year
    [InlineData("2023-01-01T00:00:00", 2023, 1, 1, 0, 0, 0)]
    [InlineData("1900-01-01", 1900, 1, 1, 0, 0, 0)] // Historic date
    [InlineData("2099-12-31 23:59:59", 2099, 12, 31, 23, 59, 59)] // Future date
    [InlineData("2023-06-15T12:00:00.000", 2023, 6, 15, 12, 0, 0)] // With milliseconds
    public void TryParseDateTime_ComplexFormats_ParsesCorrectly(string input, int expectedYear, int expectedMonth, int expectedDay, int expectedHour, int expectedMinute, int expectedSecond)
    {
        // Act
        bool result = DataTypeDetector.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.True(result, $"Failed to parse '{input}' as DateTime");
        Assert.Equal(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond), parsedDate);
    }

    [Theory]
    [InlineData("2023-02-29")] // Non-leap year February 29th
    [InlineData("2023-04-31")] // April 31st doesn't exist  
    [InlineData("2023-06-31")] // June 31st doesn't exist
    [InlineData("2023-09-31")] // September 31st doesn't exist
    [InlineData("2023-11-31")] // November 31st doesn't exist
    [InlineData("0000-01-01")] // Year 0
    [InlineData("2023-00-01")] // Month 0
    [InlineData("2023-01-00")] // Day 0
    public void TryParseDateTime_EdgeCaseInvalidDates_ReturnsFalse(string input)
    {
        // Act
        bool result = DataTypeDetector.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.False(result, $"Expected '{input}' to fail parsing but it succeeded with value: {parsedDate}");
        Assert.Equal(DateTime.MinValue, parsedDate);
    }
}
