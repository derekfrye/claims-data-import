using LibClaimsDataImport.Importer;

namespace LibClaimsDataImport.Tests;

public class MoneyParseTests
{
    [Theory]
    [InlineData("$1,234.56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("$100", 100.00)]
    [InlineData("100", 100.00)]
    [InlineData("$0.99", 0.99)]
    [InlineData("0.99", 0.99)]
    [InlineData("$1,000,000.00", 1000000.00)]
    [InlineData("1,000,000", 1000000.00)]
    [InlineData("$0", 0.00)]
    [InlineData("0", 0.00)]
    [InlineData("123.45", 123.45)]
    [InlineData("$999,999.99", 999999.99)]
    [InlineData("42", 42.00)]
    [InlineData("$5.00", 5.00)]
    public void TryParseMoney_ValidFormats_ParsesCorrectly(string input, double expectedValue)
    {
        // Arrange
        decimal expected = (decimal)expectedValue;

        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.True(result, $"Failed to parse '{input}' as money");
        Assert.Equal(expected, parsedValue);
    }

    [Theory]
    [InlineData("($1,234.56)", -1234.56)] // Parentheses for negative
    [InlineData("(1234.56)", -1234.56)]
    [InlineData("($100)", -100.00)]
    [InlineData("(100)", -100.00)]
    [InlineData("($0.99)", -0.99)]
    [InlineData("(999.99)", -999.99)]
    [InlineData("($1,000,000.00)", -1000000.00)]
    [InlineData("(0)", 0.00)] // Zero in parentheses should still be zero
    public void TryParseMoney_NegativeFormats_ParsesCorrectly(string input, double expectedValue)
    {
        // Arrange
        decimal expected = (decimal)expectedValue;

        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.True(result, $"Failed to parse negative money format '{input}'");
        Assert.Equal(expected, parsedValue);
    }

    [Theory]
    [InlineData("$ 1,234.56", 1234.56)] // Extra spaces
    [InlineData(" $1,234.56 ", 1234.56)] // Leading/trailing spaces
    [InlineData("1_234.56", 1234.56)] // Underscores
    [InlineData("$1_234.56", 1234.56)] // Dollar sign with underscores
    [InlineData("1 234.56", 1234.56)] // Spaces as separators
    [InlineData("$1 234.56", 1234.56)] // Dollar with spaces
    [InlineData(" 1,234.56 ", 1234.56)] // Whitespace around plain number
    public void TryParseMoney_FormatsWithWhitespaceAndSeparators_ParsesCorrectly(string input, double expectedValue)
    {
        // Arrange
        decimal expected = (decimal)expectedValue;

        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.True(result, $"Failed to parse money format with separators '{input}'");
        Assert.Equal(expected, parsedValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not a number")]
    [InlineData("$")]
    [InlineData("$abc")]
    [InlineData("abc$123def")] // Text mixed with numbers and symbols
    [InlineData("$123.45.67")] // Multiple decimals
    [InlineData("123..45")] // Double decimal points
    [InlineData("abc")] // Pure text
    [InlineData("$1,234.5.6")] // Invalid decimal format
    [InlineData("12.34.56")] // Multiple decimal points
    [InlineData("$12.34.56.78")] // Multiple decimal points with $
    public void TryParseMoney_InvalidFormats_ReturnsFalse(string? input)
    {
        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.False(result, $"Expected '{input}' to fail parsing but it succeeded with value: {parsedValue}");
        Assert.Equal(0, parsedValue);
    }

    [Theory]
    [InlineData("$999,999,999,999.99", 999999999999.99)] // Large amount
    [InlineData("$0.01", 0.01)] // Smallest meaningful currency unit
    [InlineData("$0.001", 0.001)] // Fractional cents
    [InlineData("$1000000000000", 1000000000000.00)] // Trillion
    [InlineData("$123456789.123456", 123456789.123456)] // Many decimal places
    [InlineData("999999999999999", 999999999999999.00)] // Large number without $
    [InlineData("$1.1", 1.1)] // Simple decimal
    public void TryParseMoney_EdgeCaseValidAmounts_ParsesCorrectly(string input, double expectedValue)
    {
        // Arrange
        decimal expected = (decimal)expectedValue;

        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.True(result, $"Failed to parse edge case money format '{input}'");
        Assert.Equal(expected, parsedValue);
    }

    [Theory]
    [InlineData("$$123.45", 123.45)] // Multiple dollar signs - current impl removes all $
    [InlineData("123$", 123.00)] // Dollar at end
    [InlineData("12$34", 1234.00)] // Dollar in middle
    [InlineData("$1,2,3,4.56", 1234.56)] // Current impl removes all commas
    [InlineData("$1,23,456.78", 123456.78)] // Current impl removes all commas
    [InlineData("$12,34,56.78", 123456.78)] // Current impl removes all commas
    [InlineData("$,123.45", 123.45)] // Leading comma removed
    [InlineData("$123,.45", 123.45)] // Comma before decimal removed
    [InlineData("$123.45,", 123.45)] // Trailing comma removed
    public void TryParseMoney_UnusualButValidFormats_ParsesCorrectly(string input, double expectedValue)
    {
        // Arrange
        decimal expected = (decimal)expectedValue;

        // Act
        bool result = FileSpec.TryParseMoney(input, out decimal parsedValue);

        // Assert
        Assert.True(result, $"Current implementation should parse '{input}' successfully");
        Assert.Equal(expected, parsedValue);
    }
}