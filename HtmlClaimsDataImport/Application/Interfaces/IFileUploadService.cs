using HtmlClaimsDataImport.Application.Commands.Requests;
using HtmlClaimsDataImport.Domain.ValueObjects;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> HandleFileUploadAsync(FileUploadRequest file, string fileType, string tempDir);
        string FormatFileSize(long bytes);
    }
}
