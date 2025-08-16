namespace HtmlClaimsDataImport.Models
{
    /// <summary>
    /// Represents the response model for a file selection operation.
    /// </summary>
    public class FileSelectedResponseModel
    {
        /// <summary>
        /// Gets or sets the type of the file.
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status message of the file selection operation.
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input identifier associated with the file.
        /// </summary>
        public string InputId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the input name the user associated with the file.
        /// </summary>
        public string InputName { get; set; } = string.Empty;
    }
}