using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;

namespace LibClaimsDataImport.Importer;

/// <summary>
/// Configuration for the import pipeline and SQLite behaviors.
/// </summary>
public class ImportConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Loads an <see cref="ImportConfig"/> from a JSON file.
    /// </summary>
    /// <param name="configPath">Path to the JSON configuration file.</param>
    /// <returns>The deserialized <see cref="ImportConfig"/> instance.</returns>
    public static ImportConfig LoadFromFile(string configPath = "ClaimsDataImportConfig.json")
    {
        if (!System.IO.File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        var jsonString = System.IO.File.ReadAllText(configPath);

        var config = JsonSerializer.Deserialize<ImportConfig>(jsonString, JsonOptions);
        return config ?? new ImportConfig();
    }

    private static string GenerateCreateTableSqlFromFileSpec(string tableName, FileSpec fileSpec)
    {
        var columns = new List<string>();

        // Add auto-increment primary key
        columns.Add("[id] INTEGER PRIMARY KEY AUTOINCREMENT");

        // Add columns based on FileSpec auto-detection
        foreach (var column in fileSpec.ColumnTypes)
        {
            var sqliteType = MapSystemTypeToSqlite(column.Value);
            var columnDef = $"[{column.Key}] {sqliteType}";
            columns.Add(columnDef);
        }

        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    private static string GetEnumCheckConstraint(List<string> enumValues, string columnName)
    {
        if (enumValues.Count == 0)
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
            "time" => "TIME",
            "datetime" => "DATETIME",
            "enum" => "TEXT", // Enum types become TEXT with CHECK constraints
            var dt when dt.StartsWith("character(") => "TEXT",
            var dt when dt.StartsWith("numeric(") => string.Concat("DECIMAL", dt.AsSpan(7)), // "numeric(10,2)" → .AsSpan(7) returns "10,2)"
            _ => "TEXT", // Default fallback
        };
    }

    private static string MapSystemTypeToSqlite(Type systemType)
    {
        if (systemType == typeof(int) || systemType == typeof(long))
        {
            return "INTEGER";
        }
        if (systemType == typeof(decimal))
        {
            return "REAL";
        }
        if (systemType == typeof(DateTime))
        {
            return "DATETIME"; // Preserve semantic type for schema readability
        }
        if (systemType == typeof(DateOnly))
        {
            return "DATE";
        }
        if (systemType == typeof(TimeOnly))
        {
            return "TIME";
        }
        return "TEXT"; // Default fallback
    }

    public SqliteSettings SqliteSettings { get; set; } = new();
    public ColumnMappings ColumnMappings { get; set; } = new();
    public ValidationSettings Validation { get; set; } = new();
    public Dictionary<string, DestinationColumn> DestinationTable { get; set; } = new();

    /// <summary>
    /// Creates the destination table if it does not exist, using either explicit schema or FileSpec autodetection.
    /// </summary>
    /// <param name="connection">An open SQLite connection.</param>
    /// <param name="tableName">The destination table name.</param>
    /// <param name="fileSpec">Optional file specification for autodetected schema.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateTableIfNotExists(SqliteConnection connection, string tableName, FileSpec? fileSpec = null)
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
        var createTableSql = this.GenerateCreateTableSql(tableName, fileSpec);

        using var command = new SqliteCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private string GenerateCreateTableSql(string tableName, FileSpec? fileSpec = null)
    {
        // Check if destination table is set to "auto" for auto-detection
        if (this.DestinationTable.Count == 1 &&
            this.DestinationTable.TryGetValue("auto", out var autoColumn) &&
            autoColumn.Datatype.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            if (fileSpec == null)
            {
                throw new InvalidOperationException("FileSpec is required when destination table is set to 'auto'");
            }

            return GenerateCreateTableSqlFromFileSpec(tableName, fileSpec);
        }

        // Validate primary key configuration for manual schema
        this.ValidatePrimaryKeyConfiguration();

        var columns = new List<string>();

        foreach (var column in this.DestinationTable)
        {
            var columnDef = $"[{column.Value.ColumnName}] {MapDataTypeToSqlite(column.Value.Datatype)}";

            if (column.Value.Nullable == "N")
            {
                columnDef += " NOT NULL";
            }

            // Handle primary key column
            if (column.Value.PrimaryKey)
            {
                columnDef += " PRIMARY KEY AUTOINCREMENT";
            }

            // Add CHECK constraint for enum types
            if (column.Value.Datatype == "enum" && column.Value.Values.Count > 0)
            {
                var checkConstraint = GetEnumCheckConstraint(column.Value.Values, column.Value.ColumnName);
                columnDef += $" {checkConstraint}";
            }

            columns.Add(columnDef);
        }

        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    private void ValidatePrimaryKeyConfiguration()
    {
        var primaryKeyColumns = this.DestinationTable.Where(c => c.Value.PrimaryKey).ToList();

        if (primaryKeyColumns.Count == 0)
        {
            throw new InvalidOperationException("No primary key column defined. Exactly one column must have primary_key: true");
        }

        if (primaryKeyColumns.Count > 1)
        {
            var columnNames = string.Join(", ", primaryKeyColumns.Select(c => c.Value.ColumnName));
            throw new InvalidOperationException($"Multiple primary key columns defined: {columnNames}. Only one column can have primary_key: true");
        }

        var primaryKeyColumn = primaryKeyColumns.First();
        if (primaryKeyColumn.Value.Datatype != "integer")
        {
            throw new InvalidOperationException($"Primary key column '{primaryKeyColumn.Value.ColumnName}' must be of integer datatype, but was '{primaryKeyColumn.Value.Datatype}'");
        }
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
    public List<string> Values { get; set; } = new();
    [JsonPropertyName("primary_key")]
    public bool PrimaryKey { get; set; } = false;
}
