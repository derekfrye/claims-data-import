namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using MediatR;

    public record UploadFileCommand(
        string fileType,
        IFormFile uploadedFile,
        string? tmpDir = null) : IRequest<FileUploadResult>;
}