namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Commands.Requests;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Mediator;

    public record UploadFileCommand(
        string FileType,
        FileUploadRequest File,
        string? TmpDir = null) : ICommand<FileUploadResult>;
}
