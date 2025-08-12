using System.Text.Json;

namespace LibClaimsDataImport.Importer;

public class ImportConfig
{
    public ConnectionSettings ConnectionSettings { get; set; } = new();
    public ImportSettings ImportSettings { get; set; } = new();
    public ColumnMappings ColumnMappings { get; set; } = new();
    public ValidationSettings Validation { get; set; } = new();

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
    public List<string> DateTimeFormats { get; set; } = new();
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
    public bool RequireAllColumns { get; set; } = false;
    public bool AllowExtraColumns { get; set; } = true;
    public int MaxRowErrors { get; set; } = 100;
}