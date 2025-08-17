namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Domain.ValueObjects;

    public interface IFileUploadService
    {
        Task<FileUploadResult> HandleFileUploadAsync(IFormFile uploadedFile, string fileType, string tempDir);
        string FormatFileSize(long bytes);
    }
}