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
        if (string.IsNullOrWhiteSpace(value))
            return typeof(string);

        // Try SQL Server Money format first (e.g., $1,234.56 or 1234.56)
        if (TryParseMoney(value, out _))
        {
            return typeof(decimal);
        }

        // Try date/time formats - check more specific types first
        if (TryParseTimeOnly(value, out _))
        {
            return typeof(TimeOnly);
        }
        else if (TryParseDateOnly(value, out _))
        {
            return typeof(DateOnly);
        }
        else if (TryParseDateTime(value, out _))
        {
            return typeof(DateTime);
        }

        // Try integer (long or int)
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue <= int.MaxValue && longValue >= int.MinValue
                ? typeof(int)
                : typeof(long);
        }

        // Default to string
        return typeof(string);
    }

    /// <summary>
    /// Parses a string value into the appropriate target type.
    /// Used during CSV data insertion to convert string values to typed objects.
    /// </summary>
    public static object ParseValue(string? value, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value ?? string.Empty;

        // Use the same parsing logic as DetectType for consistency
        if (targetType == typeof(decimal))
        {
            if (TryParseMoney(value, out var decimalValue))
                return decimalValue;
        }
        else if (targetType == typeof(DateTime))
        {
            if (TryParseDateTime(value, out var dateTimeValue))
                return dateTimeValue;
        }
        else if (targetType == typeof(DateOnly))
        {
            if (TryParseDateOnly(value, out var dateOnlyValue))
                return dateOnlyValue;
        }
        else if (targetType == typeof(TimeOnly))
        {
            if (TryParseTimeOnly(value, out var timeOnlyValue))
                return timeOnlyValue;
        }
        else if (targetType == typeof(int))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue;
        }
        else if (targetType == typeof(long))
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                return longValue;
        }

        // Default: return as string (same as original CSV value)
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