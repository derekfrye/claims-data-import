namespace LibClaimsDataImport.Importer;

public class ImportSettings
{
    public int BatchSize { get; set; } = 1000;
    public bool EnableTransactions { get; set; } = true;
    public bool ContinueOnError { get; set; } = false;
    public string LogLevel { get; set; } = "info";
}

