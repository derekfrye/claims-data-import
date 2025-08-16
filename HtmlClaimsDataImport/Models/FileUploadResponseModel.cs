namespace HtmlClaimsDataImport.Models
{
    public class FileUploadResponseModel
    {
        public string FileType { get; set; } = string.Empty;

        public string StatusMessage { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string LogEntry { get; set; } = string.Empty;

        public string InputId { get; set; } = string.Empty;

        public string InputName { get; set; } = string.Empty;
    }
}