using System.Text.Json.Serialization;

namespace LibClaimsDataImport.Importer;

public class DestinationColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string Nullable { get; set; } = "Y";
    public string Datatype { get; set; } = string.Empty;
    public IList<string> Values { get; set; } = [];
    [JsonPropertyName("primary_key")]
    public bool PrimaryKey { get; set; } = false;
}

