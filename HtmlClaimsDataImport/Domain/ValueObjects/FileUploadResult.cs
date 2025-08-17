namespace HtmlClaimsDataImport.Domain.ValueObjects
{
    public record FileUploadResult(string statusMessage, string logEntry, string filePath)
    {
        public bool IsSuccess => !string.IsNullOrEmpty(this.filePath);
    }
}