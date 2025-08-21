namespace LibClaimsDataImport.Importer;

public class ConnectionSettings
{
    public int DefaultTimeout { get; set; } = 30;
    public bool EnableForeignKeys { get; set; } = true;
    public string JournalMode { get; set; } = "WAL";
    public IDictionary<string, object> Pragma { get; set; } = new Dictionary<string, object>();
}

