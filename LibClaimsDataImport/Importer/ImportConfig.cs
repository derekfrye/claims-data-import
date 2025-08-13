using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace LibClaimsDataImport.Importer;

public class ImportConfig
{
    public SqliteSettings SqliteSettings { get; set; } = new();
    public ColumnMappings ColumnMappings { get; set; } = new();
    public ValidationSettings Validation { get; set; } = new();
    public Dictionary<string, DestinationColumn> DestinationTable { get; set; } = new();

    public static ImportConfig LoadFromFile(string configPath = "ClaimsDataImportConfig.json")
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var jsonString = File.ReadAllText(configPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<ImportConfig>(jsonString, options);
        return config ?? new ImportConfig();
    }

    public async Task CreateTableIfNotExists(SqliteConnection connection, string tableName)
    {
        // Check if table exists
        var tableExistsCommand = connection.CreateCommand();
        tableExistsCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=$table";
        tableExistsCommand.Parameters.AddWithValue("$table", tableName);
        
        var tableExists = await tableExistsCommand.ExecuteScalarAsync();
        if (tableExists != null)
        {
            return; // Table already exists
        }

        // Create table based on destinationTable configuration
        var createTableSql = GenerateCreateTableSql(tableName);
        
        using var command = new SqliteCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private string GenerateCreateTableSql(string tableName)
    {
        var columns = new List<string>();
        
        foreach (var column in DestinationTable)
        {
            var columnDef = $"[{column.Value.ColumnName}] {MapDataTypeToSqlite(column.Value.Datatype)}";
            
            if (column.Value.Nullable == "N")
            {
                columnDef += " NOT NULL";
            }

            // Handle identity column for recid
            if (column.Key == "recid" && column.Value.Datatype == "integer")
            {
                columnDef += " PRIMARY KEY AUTOINCREMENT";
            }

            // Add CHECK constraint for enum types
            if (column.Value.Datatype.EndsWith("_enum"))
            {
                var checkConstraint = GetEnumCheckConstraint(column.Value.Datatype, column.Value.ColumnName);
                if (!string.IsNullOrEmpty(checkConstraint))
                {
                    columnDef += $" {checkConstraint}";
                }
            }
            
            columns.Add(columnDef);
        }

        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    private static string GetEnumCheckConstraint(string enumType, string columnName)
    {
        var enumValues = enumType.ToLowerInvariant() switch
        {
            "pharmacy_enum" => new[] { "R", "M", "S" }, // Retail, Mail, Specialty
            "yn_enum" => new[] { "Y", "N" }, // Yes/No
            "bg_enum" => new[] { "B", "G" }, // Brand/Generic
            "acute_maint_enum" => new[] { "Acute", "Maint" }, // Acute/Maintenance
            _ => Array.Empty<string>()
        };

        if (enumValues.Length == 0)
        {
            return string.Empty;
        }

        var quotedValues = enumValues.Select(v => $"'{v}'");
        return $"CHECK ([{columnName}] IN ({string.Join(", ", quotedValues)}))";
    }

    private static string MapDataTypeToSqlite(string datatype)
    {
        return datatype.ToLowerInvariant() switch
        {
            "integer" => "INTEGER",
            "text" => "TEXT",
            "date" => "DATE",
            var dt when dt.StartsWith("character(") => "TEXT",
            var dt when dt.StartsWith("numeric(") => "DECIMAL" + dt.Substring(7), // Keep precision info
            var dt when dt.EndsWith("_enum") => "TEXT", // All enum types become TEXT with CHECK constraints
            _ => "TEXT" // Default fallback
        };
    }
}

public class SqliteSettings
{
    public ConnectionSettings ConnectionSettings { get; set; } = new();
    public ImportSettings ImportSettings { get; set; } = new();
}

public class ConnectionSettings
{
    public int DefaultTimeout { get; set; } = 30;
    public bool EnableForeignKeys { get; set; } = true;
    public string JournalMode { get; set; } = "WAL";
    public Dictionary<string, object> Pragma { get; set; } = new();
}

public class ImportSettings
{
    public int BatchSize { get; set; } = 1000;
    public bool EnableTransactions { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
    public string LogLevel { get; set; } = "info";
}

public class ColumnMappings
{
    public MoneyFormats MoneyFormats { get; set; } = new();
}

public class MoneyFormats
{
    public bool AllowParenthesesForNegative { get; set; } = true;
    public bool StripCurrencySymbols { get; set; } = true;
    public bool StripThousandsSeparators { get; set; } = true;
    public string DefaultCurrency { get; set; } = "USD";
}

public class ValidationSettings
{
    public int MaxRowErrors { get; set; } = 100;
}

public class DestinationColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string Nullable { get; set; } = "Y";
    public string Datatype { get; set; } = string.Empty;
}