namespace LibClaimsDataImport.Importer;

public class SqliteSettings
{
    public ConnectionSettings ConnectionSettings { get; set; } = new();
    public ImportSettings ImportSettings { get; set; } = new();
}

