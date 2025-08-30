using System.Globalization;

namespace LibClaimsDataImport.Importer
{
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
        /// <param name="value">The input string value to analyze.</param>
        /// <returns>The detected <see cref="Type"/> to represent the value.</returns>
        public static Type DetectType(string? value)
        {
            (Type DetectedType, _) = TryParseToStrongestType(value, out _);
            return DetectedType;
        }

        /// <summary>
        /// Parses a string value into the appropriate target type.
        /// Used during CSV data insertion to convert string values to typed objects.
        /// </summary>
        /// <param name="value">The input string value.</param>
        /// <param name="targetType">The desired target <see cref="Type"/>.</param>
        /// <returns>The parsed value as the requested type, or the original string if no conversion applies.</returns>
        public static object ParseValue(string? value, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value ?? string.Empty;
            }

            (Type DetectedType, _) = TryParseToStrongestType(value, out var parsedValue);

            // If the detected type matches the target type, return the parsed value
            if (DetectedType == targetType)
            {
                return parsedValue;
            }

            // Otherwise, try to parse specifically for the target type
            return TryParseForSpecificType(value, targetType);
        }

        /// <summary>
        /// Tries to parse a string as a time-only value (HH:mm, HH:mm:ss, etc.).
        /// </summary>
        /// <param name="value">The input string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="TimeOnly"/> if parsing succeeded; otherwise <see cref="TimeOnly.MinValue"/>.</param>
        /// <returns><c>true</c> if the input represents a time-only value; otherwise, <c>false</c>.</returns>
        public static bool TryParseTimeOnly(string? value, out TimeOnly result)
        {
            result = TimeOnly.MinValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Check if it looks like time-only format (HH:mm, HH:mm:ss, etc.)
            if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
            {
                // Additional check: ensure it doesn't contain date components
                return !value.Contains('/') && !value.Contains('-') && !char.IsLetter(value[0]);
            }
            return false;
        }

        /// <summary>
        /// Tries to parse a string as a date-only value (no time component).
        /// </summary>
        /// <param name="value">The input string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="DateOnly"/> if parsing succeeded; otherwise <see cref="DateOnly.MinValue"/>.</param>
        /// <returns><c>true</c> if the input represents a date-only value; otherwise, <c>false</c>.</returns>
        public static bool TryParseDateOnly(string? value, out DateOnly result)
        {
            result = DateOnly.MinValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Try to parse as date and check if it's date-only (no time component)
            if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out result))
            {
                // Additional check: ensure it doesn't contain time components
                return !value.Contains(':') && !value.Contains("am", StringComparison.OrdinalIgnoreCase) && !value.Contains("pm", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Tries to parse a string as a <see cref="DateTime"/> using common date/time formats.
        /// </summary>
        /// <param name="value">The input string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="DateTime"/> if parsing succeeded; otherwise <see cref="DateTime.MinValue"/>.</param>
        /// <returns><c>true</c> if the input represents a date and time; otherwise, <c>false</c>.</returns>
        public static bool TryParseDateTime(string? value, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Try parsing common date formats
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        /// <summary>
        /// Tries to parse a string as a money/decimal value, handling currency symbols, thousands separators, and parentheses for negatives.
        /// </summary>
        /// <param name="value">The input string to parse.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="decimal"/> if parsing succeeded; otherwise 0.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParseMoney(string? value, out decimal result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Remove common money formatting characters
            var cleanValue = string.Concat(value.Where(c => !char.IsWhiteSpace(c)))
                .Replace("$", "")
                .Replace(",", "")
                .Replace("_", "")
                .Replace("(", "-")
                .Replace(")", "");

            return decimal.TryParse(cleanValue, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Core parsing that detects the strongest possible type for a value and optionally returns the parsed value.
        /// </summary>
        /// <param name="value">The input string value.</param>
        /// <param name="parsedValue">Outputs the parsed value when detection succeeds.</param>
        /// <returns>A tuple of the detected type and a success flag.</returns>
        private static (Type DetectedType, bool Success) TryParseToStrongestType(string? value, out object parsedValue)
        {
            parsedValue = value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return (typeof(string), true);
            }

            // Leading-zero rule: if digits-only representation length > 1 and starts with '0', treat as string
            var digitsOnlyBuilder = new System.Text.StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    _ = digitsOnlyBuilder.Append(value[i]);
                }
            }
            var digitsOnly = digitsOnlyBuilder.ToString();
            if (digitsOnly.Length > 1 && digitsOnly[0] == '0')
            {
                parsedValue = value;
                return (typeof(string), true);
            }

            // Prefer integer for pure numeric values (allow thousands separators)
            if (long.TryParse(value, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var longValue))
            {
                if (longValue is <= int.MaxValue and >= int.MinValue)
                {
                    parsedValue = (int)longValue;
                    return (typeof(int), true);
                }

                parsedValue = longValue;
                return (typeof(long), true);
            }

            // Money/decimal detection (handles currency symbols, commas, parentheses)
            if (TryParseMoney(value, out var decimalValue))
            {
                parsedValue = decimalValue;
                return (typeof(decimal), true);
            }

            // Try date/time formats - check more specific types first
            if (TryParseTimeOnly(value, out TimeOnly timeOnlyValue))
            {
                parsedValue = timeOnlyValue;
                return (typeof(TimeOnly), true);
            }

            if (TryParseDateOnly(value, out DateOnly dateOnlyValue))
            {
                parsedValue = dateOnlyValue;
                return (typeof(DateOnly), true);
            }

            if (TryParseDateTime(value, out DateTime dateTimeValue))
            {
                parsedValue = dateTimeValue;
                return (typeof(DateTime), true);
            }

            // Integer already attempted above

            // Default to string
            parsedValue = value;
            return (typeof(string), true);
        }

        /// <summary>
        /// Attempts to parse a value for a specific target type when the auto-detected type does not match.
        /// </summary>
        /// <param name="value">The input string value.</param>
        /// <param name="targetType">The requested <see cref="Type"/> to parse into.</param>
        /// <returns>The parsed value as the target type when possible; otherwise, the original string.</returns>
        private static object TryParseForSpecificType(string value, Type targetType)
        {
            if (targetType == typeof(decimal) && TryParseMoney(value, out var decimalValue))
            {
                return decimalValue;
            }

            if (targetType == typeof(DateTime) && TryParseDateTime(value, out DateTime dateTimeValue))
            {
                return dateTimeValue;
            }

            if (targetType == typeof(DateOnly) && TryParseDateOnly(value, out DateOnly dateOnlyValue))
            {
                return dateOnlyValue;
            }

            if (targetType == typeof(TimeOnly) && TryParseTimeOnly(value, out TimeOnly timeOnlyValue))
            {
                return timeOnlyValue;
            }

            if (targetType == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                return intValue;
            }

            if (targetType == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            {
                return longValue;
            }

            // Default: return as string
            return value;
        }
    }
}
