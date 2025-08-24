namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Mediator;

    public record UploadFileCommand(
        string fileType,
        IFormFile uploadedFile,
        string? tmpDir = null) : ICommand<FileUploadResult>;
}
