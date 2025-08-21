namespace CmdClaimsDataImport;

public class ImportArguments
{
    public string DatabasePath { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string CsvFileName { get; set; } = string.Empty;
    public string? ConfigPath { get; set; }
}

