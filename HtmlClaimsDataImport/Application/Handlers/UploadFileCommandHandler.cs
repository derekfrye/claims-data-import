namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using HtmlClaimsDataImport.Infrastructure.Services;
    using MediatR;

    public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileUploadResult>
    {
        private readonly IFileUploadService fileUploadService;
        private readonly ITempDirectoryService tempDirectoryService;

        public UploadFileCommandHandler(IFileUploadService fileUploadService, ITempDirectoryService tempDirectoryService)
        {
            this.fileUploadService = fileUploadService;
            this.tempDirectoryService = tempDirectoryService;
        }

        public async Task<FileUploadResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
        {
            if (request.uploadedFile == null || request.uploadedFile.Length == 0)
            {
                return new FileUploadResult("No file selected", "", "");
            }

            // Use specified temp directory if provided, otherwise use session temp directory
            string tempDir;
            if (!string.IsNullOrEmpty(request.tmpDir))
            {
                // Security validation: ensure tmpdir is within authorized base path
                var authorizedBasePath = TempDirectoryCleanupService.GetTempBasePath();
                var normalizedTmpdir = Path.GetFullPath(request.tmpDir);
                var normalizedBasePath = Path.GetFullPath(authorizedBasePath);

                if (!normalizedTmpdir.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠️  SECURITY WARNING: tmpdir parameter '{request.tmpDir}' is outside authorized base path '{authorizedBasePath}'. Reverting to session-based logic.");
                    tempDir = this.tempDirectoryService.GetSessionTempDirectory();
                }
                else
                {
                    tempDir = request.tmpDir;
                    // Ensure the directory exists if tmpdir was specified and validated
                    if (!Directory.Exists(request.tmpDir))
                    {
                        Directory.CreateDirectory(request.tmpDir);
                    }
                }
            }
            else
            {
                tempDir = this.tempDirectoryService.GetSessionTempDirectory();
            }

            return await this.fileUploadService.HandleFileUploadAsync(request.uploadedFile, request.fileType, tempDir).ConfigureAwait(false);
        }
    }
}