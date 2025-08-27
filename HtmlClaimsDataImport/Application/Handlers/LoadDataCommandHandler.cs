namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Commands.Results;
    using HtmlClaimsDataImport.Application.Interfaces;
    using Mediator;
    using Microsoft.Extensions.Logging;

    public class LoadDataCommandHandler : ICommandHandler<LoadDataCommand, LoadDataResult>
    {
        private readonly IValidationService validationService;
        private readonly IDataImportService dataImportService;
        private readonly ILogger<LoadDataCommandHandler> logger;

        public LoadDataCommandHandler(IValidationService validationService, IDataImportService dataImportService, ILogger<LoadDataCommandHandler> logger)
        {
            this.validationService = validationService;
            this.dataImportService = dataImportService;
            this.logger = logger;
        }

        public async ValueTask<LoadDataResult> Handle(LoadDataCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve paths
                string actualJsonPath = ResolveJsonPath(request.jsonPath, request.tmpDir);
                string actualDatabasePath = await this.dataImportService.ResolveActualPath(request.databasePath, request.tmpDir, "default.sqlite3.db").ConfigureAwait(false);
                string actualFileName = Path.Combine(request.tmpDir, request.fileName);

                // Validation step 1: Check if JSON is valid
                var jsonValidation = await this.validationService.ValidateJsonFileAsync(actualJsonPath).ConfigureAwait(false);
                if (!jsonValidation.isValid)
                {
                    return LoadDataResult.Fail($"json invalid: {jsonValidation.errorMessage}");
                }

                // Validation step 2: Check if file exists and is readable
                var fileValidation = await this.validationService.ValidateFileAsync(actualFileName).ConfigureAwait(false);
                if (!fileValidation.isValid)
                {
                    return LoadDataResult.Fail(fileValidation.errorMessage);
                }

                // Validation step 3: Check if database exists and is readable as SQLite
                var dbValidation = await this.validationService.ValidateSqliteDatabaseAsync(actualDatabasePath).ConfigureAwait(false);
                if (!dbValidation.isValid)
                {
                    return LoadDataResult.Fail(dbValidation.errorMessage);
                }

                // All validations passed - proceed with import
                return await this.dataImportService.ProcessFileImport(actualFileName, actualJsonPath, actualDatabasePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in LoadData");
                return LoadDataResult.Fail($"Error: {ex.Message}");
            }
        }

        private static string ResolveJsonPath(string path, string tmpdir)
        {
            if (string.Equals(path, "default", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "default.json");
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
        }
    }
}
