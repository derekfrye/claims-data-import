using Sylvan.Data.Csv;
using System.Globalization;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LibClaimsDataImport.Tests")]

namespace LibClaimsDataImport.Importer;

public class FileSpec
{
    private readonly CsvDataReader _csvReader;
    
    public Dictionary<string, Type> ColumnTypes { get; private set; } = new();
    public List<string> ColumnNames { get; private set; } = new();

    public FileSpec(CsvDataReader csvReader)
    {
        _csvReader = csvReader ?? throw new ArgumentNullException(nameof(csvReader));
    }

    public void Scan()
    {
        // Get column names from header and sanitize them
        var columnCount = _csvReader.FieldCount;
        var columnNames = new string[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            columnNames[i] = SanitizeColumnName(_csvReader.GetName(i));
        }
        ColumnNames.AddRange(columnNames);

        // Initialize column type arrays
        var columnTypes = new TypeCode[columnCount];
        var hasData = new bool[columnCount];
        
        // Initialize all columns as unknown
        for (int i = 0; i < columnCount; i++)
        {
            columnTypes[i] = TypeCode.Empty;
        }

        // Read through all records to determine column types
        while (_csvReader.Read())
        {
            for (int i = 0; i < columnCount; i++)
            {
                if (_csvReader.IsDBNull(i))
                    continue;

                var value = _csvReader.GetString(i);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                hasData[i] = true;

                // Skip if we've already determined this is a string column
                if (columnTypes[i] == TypeCode.String)
                    continue;

                // Try to determine the most appropriate type
                var detectedType = DetectColumnType(value);
                
                // If this is the first non-null value, set the type
                if (columnTypes[i] == TypeCode.Empty)
                {
                    columnTypes[i] = detectedType;
                }
                // If we detect a different type, demote to string
                else if (columnTypes[i] != detectedType)
                {
                    columnTypes[i] = TypeCode.String;
                }
            }
        }

        // Build the column types dictionary 
        for (int i = 0; i < columnCount; i++)
        {
            var columnType = hasData[i] ? ConvertToSystemType(columnTypes[i]) : typeof(string);
            ColumnTypes[columnNames[i]] = columnType;
        }
    }

    private static TypeCode DetectColumnType(string value)
    {
        // Try SQL Server Money format first (e.g., $1,234.56 or 1234.56)
        if (TryParseMoney(value, out _))
        {
            return TypeCode.Decimal;
        }

        // Try date/time formats
        if (TryParseDateTime(value, out _))
        {
            return TypeCode.DateTime;
        }

        // Try integer (long or int)
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue <= int.MaxValue && longValue >= int.MinValue
                ? TypeCode.Int32
                : TypeCode.Int64;
        }

        // Default to string
        return TypeCode.String;
    }

    internal static bool TryParseDateTime(string? value, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Try parsing common date formats
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    internal static bool TryParseMoney(string? value, out decimal result)
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

    private static Type ConvertToSystemType(TypeCode typeCode)
    {
        return typeCode switch
        {
            TypeCode.Int32 => typeof(int),
            TypeCode.Int64 => typeof(long),
            TypeCode.Decimal => typeof(decimal),
            TypeCode.DateTime => typeof(DateTime),
            _ => typeof(string)
        };
    }

    private static string SanitizeColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return "column";

        // Trim whitespace
        var sanitized = columnName.Trim();
        
        // Remove non-ASCII characters and convert non-alphanumeric to underscores
        var result = new System.Text.StringBuilder();
        foreach (char c in sanitized)
        {
            if (char.IsAsciiLetter(c) || char.IsAsciiDigit(c))
            {
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append('_');
            }
        }
        
        var finalResult = result.ToString();
        
        // Ensure it doesn't start with a number or underscore
        if (string.IsNullOrEmpty(finalResult) || char.IsAsciiDigit(finalResult[0]))
        {
            finalResult = "col_" + finalResult;
        }
        
        // Remove consecutive underscores
        while (finalResult.Contains("__"))
        {
            finalResult = finalResult.Replace("__", "_");
        }
        
        // Remove trailing underscores
        finalResult = finalResult.TrimEnd('_');
        
        return string.IsNullOrEmpty(finalResult) ? "column" : finalResult;
    }
}