namespace HtmlClaimsDataImport.Application.Queries.Dtos
{
    /// <summary>
    /// Application-level DTO for preview data returned by query handlers.
    /// Keeps read model independent from UI view models.
    /// </summary>
    public class PreviewDataDto
    {
        public string StatusMessage { get; set; } = string.Empty;

        public bool IsPreviewAvailable { get; set; }

        public IList<string> ImportColumns { get; set; } = new List<string>();

        public IList<string> ClaimsColumns { get; set; } = new List<string>();

        public IList<Dictionary<string, string>> PreviewRows { get; set; } = new List<Dictionary<string, string>>();

        public string ImportTableName { get; set; } = string.Empty;

        public int CurrentMappingStep { get; set; } = 0;

        public IDictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public string SelectedImportColumn { get; set; } = string.Empty;
    }
}

