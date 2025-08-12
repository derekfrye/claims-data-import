using LibClaimsDataImport.Importer;

namespace LibClaimsDataImport.Tests;

public class FileSpecTests
{
    [Theory]
    [InlineData("2023-12-25", true)]
    [InlineData("12/25/2023", true)]
    [InlineData("2023-12-25 14:30:00", true)]
    [InlineData("Dec 25, 2023", true)]
    [InlineData("25 Dec 2023", true)]
    [InlineData("2023/12/25", true)]
    [InlineData("12-25-2023", true)]
    [InlineData("2023-12-25T14:30:00", true)]
    [InlineData("2023-12-25T14:30:00Z", true)]
    [InlineData("Monday, December 25, 2023", true)]
    public void TryParseDateTime_ValidDateFormats_ReturnsTrue(string input, bool expected)
    {
        // Act
        bool result = FileSpec.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.NotEqual(DateTime.MinValue, parsedDate);
        }
    }

    [Theory]
    [InlineData("not a date", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("123456", false)]
    [InlineData("abc-def-ghi", false)]
    [InlineData("25/13/2023", false)] // Invalid month
    [InlineData("32/12/2023", false)] // Invalid day
    [InlineData("2023-13-01", false)] // Invalid month
    [InlineData("2023-12-32", false)] // Invalid day
    [InlineData("25:30:70", false)] // Invalid time format
    public void TryParseDateTime_InvalidDateFormats_ReturnsFalse(string? input, bool expected)
    {
        // Act
        bool result = FileSpec.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.Equal(expected, result);
        if (!expected)
        {
            Assert.Equal(DateTime.MinValue, parsedDate);
        }
    }

    [Fact]
    public void TryParseDateTime_SpecificValidDate_ParsesCorrectly()
    {
        // Arrange
        string input = "2023-12-25 15:30:45";
        
        // Act
        bool result = FileSpec.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.True(result);
        Assert.Equal(new DateTime(2023, 12, 25, 15, 30, 45), parsedDate);
    }

    [Fact]
    public void TryParseDateTime_ISO8601Format_ParsesCorrectly()
    {
        // Arrange
        string input = "2023-12-25T15:30:45";
        
        // Act
        bool result = FileSpec.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.True(result);
        Assert.Equal(2023, parsedDate.Year);
        Assert.Equal(12, parsedDate.Month);
        Assert.Equal(25, parsedDate.Day);
        Assert.Equal(15, parsedDate.Hour);
        Assert.Equal(30, parsedDate.Minute);
        Assert.Equal(45, parsedDate.Second);
    }

    [Theory]
    [InlineData("1/1/2000")]
    [InlineData("01/01/2000")]
    [InlineData("2000-01-01")]
    [InlineData("Jan 1, 2000")]
    public void TryParseDateTime_VariousFormatsForSameDate_AllSucceed(string input)
    {
        // Act
        bool result = FileSpec.TryParseDateTime(input, out DateTime parsedDate);

        // Assert
        Assert.True(result);
        Assert.Equal(2000, parsedDate.Year);
        Assert.Equal(1, parsedDate.Month);
        Assert.Equal(1, parsedDate.Day);
    }
}
