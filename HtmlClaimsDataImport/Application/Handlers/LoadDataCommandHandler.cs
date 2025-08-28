using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Commands.Results;
using HtmlClaimsDataImport.Application.Interfaces;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class LoadDataCommandHandler(IValidationService validationService, IDataImportService dataImportService, ILogger<LoadDataCommandHandler> logger) : ICommandHandler<LoadDataCommand, LoadDataResult>
    {
        private readonly IValidationService validationService = validationService;
        private readonly IDataImportService dataImportService = dataImportService;
        private readonly ILogger<LoadDataCommandHandler> logger = logger;

        public async ValueTask<LoadDataResult> Handle(LoadDataCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Resolve paths
                var actualJsonPath = ResolveJsonPath(request.JsonPath, request.TmpDir);
                var actualDatabasePath = await dataImportService.ResolveActualPath(request.DatabasePath, request.TmpDir, "default.sqlite3.db").ConfigureAwait(false);
                var actualFileName = Path.Combine(request.TmpDir, request.FileName);

                // Validation step 1: Check if JSON is valid
                Domain.ValueObjects.ValidationResult jsonValidation = await validationService.ValidateJsonFileAsync(actualJsonPath).ConfigureAwait(false);
                if (!jsonValidation.IsValid)
                {
                    return LoadDataResult.Fail($"json invalid: {jsonValidation.ErrorMessage}");
                }

                // Validation step 2: Check if file exists and is readable
                Domain.ValueObjects.ValidationResult fileValidation = await validationService.ValidateFileAsync(actualFileName).ConfigureAwait(false);
                if (!fileValidation.IsValid)
                {
                    return LoadDataResult.Fail(fileValidation.ErrorMessage);
                }

                // Validation step 3: Check if database exists and is readable as SQLite
                Domain.ValueObjects.ValidationResult dbValidation = await validationService.ValidateSqliteDatabaseAsync(actualDatabasePath).ConfigureAwait(false);
                if (!dbValidation.IsValid)
                {
                    return LoadDataResult.Fail(dbValidation.ErrorMessage);
                }

                // All validations passed - proceed with import
                return await dataImportService.ProcessFileImport(actualFileName, actualJsonPath, actualDatabasePath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoadData");
                return LoadDataResult.Fail($"Error: {ex.Message}");
            }
        }

        private static string ResolveJsonPath(string path, string tmpdir)
        {
            return string.Equals(path, "default", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(Directory.GetCurrentDirectory(), "default.json")
                : Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
        }
    }
}
