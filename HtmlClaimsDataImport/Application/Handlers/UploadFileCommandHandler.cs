using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Domain.ValueObjects;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class UploadFileCommandHandler(IFileUploadService fileUploadService, ITempDirectoryService tempDirectoryService) : ICommandHandler<UploadFileCommand, FileUploadResult>
    {
        private readonly IFileUploadService fileUploadService = fileUploadService;
        private readonly ITempDirectoryService tempDirectoryService = tempDirectoryService;

        public async ValueTask<FileUploadResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return new FileUploadResult("No file selected", "", "");
            }

            // Resolve which temp directory to use with policy encapsulated in temp service
            var tempDir = tempDirectoryService.ResolveUploadTempDirectory(request.TmpDir);

            return await fileUploadService.HandleFileUploadAsync(request.File, request.FileType, tempDir).ConfigureAwait(false);
        }
    }
}
