namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Application.Commands.Requests;
    using HtmlClaimsDataImport.Domain.ValueObjects;

    public interface IFileUploadService
    {
        Task<FileUploadResult> HandleFileUploadAsync(FileUploadRequest file, string fileType, string tempDir);
        string FormatFileSize(long bytes);
    }
}
