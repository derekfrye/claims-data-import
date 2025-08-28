namespace HtmlClaimsDataImport.Domain.ValueObjects
{
    public record FileUploadResult(string StatusMessage, string LogEntry, string FilePath)
    {
        public bool IsSuccess => !string.IsNullOrEmpty(FilePath);
    }
}
