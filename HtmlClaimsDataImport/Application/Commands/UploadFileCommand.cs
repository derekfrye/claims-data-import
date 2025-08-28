using HtmlClaimsDataImport.Application.Commands.Requests;
using HtmlClaimsDataImport.Domain.ValueObjects;
using Mediator;

namespace HtmlClaimsDataImport.Application.Commands
{
    public record UploadFileCommand(
        string FileType,
        FileUploadRequest File,
        string? TmpDir = null) : ICommand<FileUploadResult>;
}
