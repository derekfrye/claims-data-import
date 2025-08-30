using System.Runtime.CompilerServices;
using System.Text;
using Sylvan.Data.Csv;

[assembly: InternalsVisibleTo("LibClaimsDataImport.Tests")]

namespace LibClaimsDataImport.Importer
{
    /// <summary>
    /// Scans CSV headers and data to infer column names and types.
    /// </summary>
    public class FileSpec
    {
        private readonly CsvDataReader csvReader;
        private readonly Dictionary<string, Type> columnTypes = new(StringComparer.Ordinal);
        private readonly List<string> columnNames = [];

        public IReadOnlyDictionary<string, Type> ColumnTypes => columnTypes;
        public IReadOnlyList<string> ColumnNames => columnNames;

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
            var headerNames = GetSanitizedHeaderNames();
            var columnCount = headerNames.Length;
            columnNames.AddRange(headerNames);

            (TypeCode[] typeCodes, var hasData) = AnalyzeTypes(columnCount);

            for (var i = 0; i < columnCount; i++)
            {
                Type columnType = hasData[i] ? ConvertToSystemType(typeCodes[i]) : typeof(string);
                columnTypes[headerNames[i]] = columnType;
            }
        }

        private string[] GetSanitizedHeaderNames()
        {
            var columnCount = csvReader.FieldCount;
            var headerNames = new string[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                headerNames[i] = SanitizeColumnName(csvReader.GetName(i));
            }
            return headerNames;
        }

        private (TypeCode[] typeCodes, bool[] hasData) AnalyzeTypes(int columnCount)
        {
            var typeCodes = new TypeCode[columnCount];
            var hasData = new bool[columnCount];

            for (var i = 0; i < columnCount; i++)
            {
                typeCodes[i] = TypeCode.Empty;
            }

            while (csvReader.Read())
            {
                for (var i = 0; i < columnCount; i++)
                {
                    if (csvReader.IsDBNull(i))
                    {
                        continue;
                    }

                    var value = csvReader.GetString(i);
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    hasData[i] = true;

                    if (typeCodes[i] == TypeCode.String)
                    {
                        continue;
                    }

                    TypeCode detectedType = TypeCodeFromSystemType(DataTypeDetector.DetectType(value));

                    if (typeCodes[i] == TypeCode.Empty)
                    {
                        typeCodes[i] = detectedType;
                    }
                    // if we have conflicting types, treat it as string
                    else if (typeCodes[i] != detectedType)
                    {
                        typeCodes[i] = TypeCode.String;
                    }
                }
            }

            return (typeCodes, hasData);
        }

        private static Type ConvertToSystemType(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.Int32 => typeof(int),
                TypeCode.Int64 => typeof(long),
                TypeCode.Decimal => typeof(decimal),
                TypeCode.DateTime => typeof(DateTime),
                // For all other codes we don't emit from detection, default to string.
                // This avoids unexpected NotImplementedException when future codes appear.
                TypeCode.Empty => typeof(string),
                TypeCode.Object => typeof(string),
                TypeCode.DBNull => typeof(string),
                TypeCode.Boolean => typeof(string),
                TypeCode.Char => typeof(string),
                TypeCode.SByte => typeof(string),
                TypeCode.Byte => typeof(string),
                TypeCode.Int16 => typeof(string),
                TypeCode.UInt16 => typeof(string),
                TypeCode.UInt32 => typeof(string),
                TypeCode.UInt64 => typeof(string),
                TypeCode.Single => typeof(string),
                TypeCode.Double => typeof(string),
                TypeCode.String => typeof(string),
                _ => typeof(string),
            };
        }

        private static TypeCode TypeCodeFromSystemType(Type systemType)
        {
            return systemType == typeof(int)
                ? TypeCode.Int32
                : systemType == typeof(long)
                ? TypeCode.Int64
                : systemType == typeof(decimal)
                ? TypeCode.Decimal
                : systemType == typeof(DateTime) || systemType == typeof(DateOnly) || systemType == typeof(TimeOnly)
                ? TypeCode.DateTime
                : TypeCode.String;
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
            foreach (var c in sanitized)
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
