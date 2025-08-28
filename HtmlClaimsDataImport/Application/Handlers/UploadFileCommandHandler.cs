namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Mediator;

    public class UploadFileCommandHandler : ICommandHandler<UploadFileCommand, FileUploadResult>
    {
        private readonly IFileUploadService fileUploadService;
        private readonly ITempDirectoryService tempDirectoryService;

        public UploadFileCommandHandler(IFileUploadService fileUploadService, ITempDirectoryService tempDirectoryService)
        {
            this.fileUploadService = fileUploadService;
            this.tempDirectoryService = tempDirectoryService;
        }

        public async ValueTask<FileUploadResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return new FileUploadResult("No file selected", "", "");
            }

            // Resolve which temp directory to use with policy encapsulated in temp service
            string tempDir = this.tempDirectoryService.ResolveUploadTempDirectory(request.TmpDir);

            return await this.fileUploadService.HandleFileUploadAsync(request.File, request.FileType, tempDir).ConfigureAwait(false);
        }
    }
}
