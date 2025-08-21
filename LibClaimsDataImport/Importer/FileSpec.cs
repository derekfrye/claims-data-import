using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("LibClaimsDataImport.Tests")]

namespace LibClaimsDataImport.Importer
{
    using Sylvan.Data.Csv;

    /// <summary>
    /// Scans CSV headers and data to infer column names and types.
    /// </summary>
    public class FileSpec
    {
        private readonly CsvDataReader csvReader;
        private readonly Dictionary<string, Type> columnTypes = new();
        private readonly List<string> columnNames = new();

        public IReadOnlyDictionary<string, Type> ColumnTypes => this.columnTypes;
        public IReadOnlyList<string> ColumnNames => this.columnNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSpec"/> class.
        /// </summary>
        /// <param name="csvReader">The CSV reader positioned at the start of the file.</param>
        public FileSpec(CsvDataReader csvReader)
        {
            this.csvReader = csvReader ?? throw new ArgumentNullException(nameof(csvReader));
        }

        /// <summary>
        /// Scans the CSV to populate <see cref="ColumnNames"/> and inferred <see cref="ColumnTypes"/>.
        /// </summary>
        public void Scan()
        {
            // Get column names from header and sanitize them
            var columnCount = this.csvReader.FieldCount;
            var headerNames = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                headerNames[i] = SanitizeColumnName(this.csvReader.GetName(i));
            }
            this.columnNames.AddRange(headerNames);

            // Initialize column type arrays
            var typeCodes = new TypeCode[columnCount];
            var hasData = new bool[columnCount];
            
            // Initialize all columns as unknown
            for (int i = 0; i < columnCount; i++)
            {
                typeCodes[i] = TypeCode.Empty;
            }

            // Read through all records to determine column types
            while (this.csvReader.Read())
            {
                for (int i = 0; i < columnCount; i++)
                {
                    if (this.csvReader.IsDBNull(i))
                    {
                        continue;
                    }

                    var value = this.csvReader.GetString(i);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    hasData[i] = true;

                    // Skip if we've already determined this is a string column
                    if (typeCodes[i] == TypeCode.String)
                    {
                        continue;
                    }

                    // Try to determine the most appropriate type
                    var detectedType = TypeCodeFromSystemType(DataTypeDetector.DetectType(value));
                    
                    // If this is the first non-null value, set the type
                    if (typeCodes[i] == TypeCode.Empty)
                    {
                        typeCodes[i] = detectedType;
                    }
                    // If we detect a different type, demote to string
                    else if (typeCodes[i] != detectedType)
                    {
                        typeCodes[i] = TypeCode.String;
                    }
                }
            }

            // Build the column types dictionary 
            for (int i = 0; i < columnCount; i++)
            {
                var columnType = hasData[i] ? ConvertToSystemType(typeCodes[i]) : typeof(string);
                this.columnTypes[headerNames[i]] = columnType;
            }
        }

        private static Type ConvertToSystemType(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.Int32 => typeof(int),
                TypeCode.Int64 => typeof(long),
                TypeCode.Decimal => typeof(decimal),
                TypeCode.DateTime => typeof(DateTime),
                _ => typeof(string),
            };
        }

        private static TypeCode TypeCodeFromSystemType(Type systemType)
        {
            if (systemType == typeof(int))
            {
                return TypeCode.Int32;
            }
            if (systemType == typeof(long))
            {
                return TypeCode.Int64;
            }
            if (systemType == typeof(decimal))
            {
                return TypeCode.Decimal;
            }
            if (systemType == typeof(DateTime) || systemType == typeof(DateOnly) || systemType == typeof(TimeOnly))
            {
                return TypeCode.DateTime;
            }
            return TypeCode.String;
        }

        private static string SanitizeColumnName(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                return "column";
            }

        // Trim whitespace
            var sanitized = columnName.Trim();
        
        // Remove non-ASCII characters and convert non-alphanumeric to underscores
            var result = new StringBuilder();
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
}
