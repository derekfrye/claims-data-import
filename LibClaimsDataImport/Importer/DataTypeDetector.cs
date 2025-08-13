using System.Globalization;

namespace LibClaimsDataImport.Importer;

/// <summary>
/// Centralized data type detection and parsing logic to ensure consistency
/// between column type detection during scanning and value parsing during insertion.
/// </summary>
public static class DataTypeDetector
{
    /// <summary>
    /// Detects the most appropriate .NET type for a given string value.
    /// Used during CSV scanning to determine column types.
    /// </summary>
    public static Type DetectType(string? value)
    {
        var result = TryParseToStrongestType(value, out _);
        return result.DetectedType;
    }

    /// <summary>
    /// Parses a string value into the appropriate target type.
    /// Used during CSV data insertion to convert string values to typed objects.
    /// </summary>
    public static object ParseValue(string? value, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        var result = TryParseToStrongestType(value, out var parsedValue);
        
        // If the detected type matches the target type, return the parsed value
        if (result.DetectedType == targetType)
            return parsedValue;

        // Otherwise, try to parse specifically for the target type
        return TryParseForSpecificType(value, targetType);
    }

    /// <summary>
    /// Core parsing logic that detects the strongest possible type for a value
    /// and optionally returns the parsed value.
    /// </summary>
    private static (Type DetectedType, bool Success) TryParseToStrongestType(string? value, out object parsedValue)
    {
        parsedValue = value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return (typeof(string), true);

        // Try SQL Server Money format first (e.g., $1,234.56 or 1234.56)
        if (TryParseMoney(value, out var decimalValue))
        {
            parsedValue = decimalValue;
            return (typeof(decimal), true);
        }

        // Try date/time formats - check more specific types first
        if (TryParseTimeOnly(value, out var timeOnlyValue))
        {
            parsedValue = timeOnlyValue;
            return (typeof(TimeOnly), true);
        }
        
        if (TryParseDateOnly(value, out var dateOnlyValue))
        {
            parsedValue = dateOnlyValue;
            return (typeof(DateOnly), true);
        }
        
        if (TryParseDateTime(value, out var dateTimeValue))
        {
            parsedValue = dateTimeValue;
            return (typeof(DateTime), true);
        }

        // Try integer (long or int)
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            if (longValue <= int.MaxValue && longValue >= int.MinValue)
            {
                parsedValue = (int)longValue;
                return (typeof(int), true);
            }
            else
            {
                parsedValue = longValue;
                return (typeof(long), true);
            }
        }

        // Default to string
        parsedValue = value;
        return (typeof(string), true);
    }

    /// <summary>
    /// Attempts to parse a value for a specific target type when the auto-detected type doesn't match.
    /// </summary>
    private static object TryParseForSpecificType(string value, Type targetType)
    {
        if (targetType == typeof(decimal) && TryParseMoney(value, out var decimalValue))
            return decimalValue;
        
        if (targetType == typeof(DateTime) && TryParseDateTime(value, out var dateTimeValue))
            return dateTimeValue;
        
        if (targetType == typeof(DateOnly) && TryParseDateOnly(value, out var dateOnlyValue))
            return dateOnlyValue;
        
        if (targetType == typeof(TimeOnly) && TryParseTimeOnly(value, out var timeOnlyValue))
            return timeOnlyValue;
        
        if (targetType == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            return intValue;
        
        if (targetType == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            return longValue;

        // Default: return as string
        return value;
    }

    /// <summary>
    /// Tries to parse a string as a time-only value (HH:mm, HH:mm:ss, etc.)
    /// </summary>
    public static bool TryParseTimeOnly(string? value, out TimeOnly result)
    {
        result = TimeOnly.MinValue;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Check if it looks like time-only format (HH:mm, HH:mm:ss, etc.)
        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            // Additional check: ensure it doesn't contain date components
            return !value.Contains('/') && !value.Contains('-') && !char.IsLetter(value[0]);
        }
        return false;
    }

    /// <summary>
    /// Tries to parse a string as a date-only value (no time component)
    /// </summary>
    public static bool TryParseDateOnly(string? value, out DateOnly result)
    {
        result = DateOnly.MinValue;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Try to parse as date and check if it's date-only (no time component)
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
        {
            // Additional check: ensure it doesn't contain time components
            return !value.Contains(':') && !value.ToLower().Contains("am") && !value.ToLower().Contains("pm");
        }
        return false;
    }

    /// <summary>
    /// Tries to parse a string as a DateTime using common date/time formats
    /// </summary>
    public static bool TryParseDateTime(string? value, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Try parsing common date formats
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Tries to parse a string as a money/decimal value, handling various formats
    /// including currency symbols, thousands separators, and parentheses for negatives
    /// </summary>
    public static bool TryParseMoney(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Remove common money formatting characters
        var cleanValue = string.Concat(value.Where(c => !char.IsWhiteSpace(c)))
            .Replace("$", "")
            .Replace(",", "")
            .Replace("_", "")
            .Replace("(", "-")
            .Replace(")", "");

        return decimal.TryParse(cleanValue, NumberStyles.Number | NumberStyles.AllowCurrencySymbol,
            CultureInfo.InvariantCulture, out result);
    }
}