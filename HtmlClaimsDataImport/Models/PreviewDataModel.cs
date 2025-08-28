namespace HtmlClaimsDataImport.Models
{
    /// <summary>
    /// Model representing preview data for the column mapping interface.
    /// </summary>
    public class PreviewDataModel
    {
        /// <summary>
        /// Gets or sets the status message to display.
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the preview data is available.
        /// </summary>
        public bool IsPreviewAvailable { get; set; }

        /// <summary>
        /// Gets or sets the column names from the import table.
        /// </summary>
        public IList<string> ImportColumns { get; set; } = [];

        /// <summary>
        /// Gets or sets the column names from the claims table.
        /// </summary>
        public IList<string> ClaimsColumns { get; set; } = [];

        /// <summary>
        /// Gets or sets the preview data rows (first 10 rows).
        /// </summary>
        public IList<Dictionary<string, string>> PreviewRows { get; set; } = [];

        /// <summary>
        /// Gets or sets the name of the import table found.
        /// </summary>
        public string ImportTableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current column mapping step (0-based index into ClaimsColumns).
        /// </summary>
        public int CurrentMappingStep { get; set; } = 0;

        /// <summary>
        /// Gets or sets the current column mappings (ClaimsColumn -> ImportColumn).
        /// </summary>
        public IDictionary<string, string> ColumnMappings { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the currently selected import column for highlighting.
        /// </summary>
        public string SelectedImportColumn { get; set; } = string.Empty;
    }
}
