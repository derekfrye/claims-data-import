namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Interfaces;
    using MediatR;

    public class LoadDataCommandHandler : IRequestHandler<LoadDataCommand, string>
    {
        private readonly IValidationService validationService;
        private readonly IDataImportService dataImportService;

        public LoadDataCommandHandler(IValidationService validationService, IDataImportService dataImportService)
        {
            this.validationService = validationService;
            this.dataImportService = dataImportService;
        }

        public async Task<string> Handle(LoadDataCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve paths
                string actualJsonPath = ResolveJsonPath(request.jsonPath, request.tmpDir);
                string actualDatabasePath = await this.dataImportService.ResolveActualPath(request.databasePath, request.tmpDir, "default.sqlite3.db").ConfigureAwait(false);
                string actualFileName = Path.Combine(request.tmpDir, request.fileName);

                // Validation step 1: Check if JSON is valid
                var jsonValidation = this.validationService.ValidateJsonFile(actualJsonPath);
                if (!jsonValidation.isValid)
                {
                    return $"json invalid: {jsonValidation.errorMessage}";
                }

                // Validation step 2: Check if file exists and is readable
                var fileValidation = this.validationService.ValidateFile(actualFileName);
                if (!fileValidation.isValid)
                {
                    return fileValidation.errorMessage;
                }

                // Validation step 3: Check if database exists and is readable as SQLite
                var dbValidation = this.validationService.ValidateSqliteDatabase(actualDatabasePath);
                if (!dbValidation.isValid)
                {
                    return dbValidation.errorMessage;
                }

                // All validations passed - proceed with import
                return await this.dataImportService.ProcessFileImport(actualFileName, actualJsonPath, actualDatabasePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadData: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static string ResolveJsonPath(string path, string tmpdir)
        {
            if (path == "default")
            {
                return Path.Combine(Directory.GetCurrentDirectory(), "default.json");
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
        }
    }
}