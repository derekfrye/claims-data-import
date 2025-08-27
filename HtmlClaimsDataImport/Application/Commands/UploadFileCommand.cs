namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Commands.Requests;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Mediator;

    public record UploadFileCommand(
        string fileType,
        FileUploadRequest file,
        string? tmpDir = null) : ICommand<FileUploadResult>;
}
