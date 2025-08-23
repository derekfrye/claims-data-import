namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Commands.Results;
    using HtmlClaimsDataImport.Application.Interfaces;
    using LibClaimsDataImport.Importer;
    using Sylvan.Data.Csv;
    using Microsoft.Extensions.Logging;

    public class DataImportService : IDataImportService
    {
        private readonly ILogger<DataImportService> logger;

        public DataImportService(ILogger<DataImportService> logger)
        {
            this.logger = logger;
        }
        public async Task<string> ResolveActualPath(string path, string tmpdir, string defaultFileName)
        {
            if (string.Equals(path, "default", StringComparison.OrdinalIgnoreCase))
            {
                // Copy default file to temp directory
                string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);
                string tempPath = Path.Combine(tmpdir, "working_db.db");

                // Copy the default file to temp directory
                await CopyFileAsync(defaultPath, tempPath).ConfigureAwait(false);
                return tempPath;
            }
            else
            {
                // For uploaded files, copy to temp directory with a working copy name
                string sourcePath = Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
                string tempPath = Path.Combine(tmpdir, "working_db.db");

                // Copy the uploaded file to a working copy
                await CopyFileAsync(sourcePath, tempPath).ConfigureAwait(false);
                return tempPath;
            }
        }

        public async Task<LoadDataResult> ProcessFileImport(string fileName, string jsonPath, string databasePath)
        {
            try
            {
                // Step 4a: Setup stream reader
                using var streamReader = new StreamReader(fileName);

                // Step 4b: Setup FileSpec and scan
                var scanCsvReader = CsvDataReader.Create(streamReader, new CsvDataReaderOptions { HasHeaders = true });
                var fileSpec = new FileSpec(scanCsvReader);
                fileSpec.Scan();

                // Step 4c: Reset stream for import
                streamReader.BaseStream.Position = 0;

                // Step 4d: Setup ImportConfig
                ImportConfig? config = null;
                if (!string.Equals(jsonPath, "default", StringComparison.OrdinalIgnoreCase))
                {
                    config = ImportConfig.LoadFromFile(jsonPath);
                }

                // Step 4e: Setup File and import
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                streamReader.DiscardBufferedData();
                var file = new LibClaimsDataImport.Importer.File(streamReader, fileSpec, config);

                // Generate a unique table name for import
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var importTableName = $"claims_import_{timestamp}";

                // Import to database (WriteToDb handles transactions internally)
                await file.WriteToDb(databasePath, importTableName).ConfigureAwait(false);

                return LoadDataResult.Ok(importTableName, $"file imported to table '{importTableName}' in temp database");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in ProcessFileImport");
                return LoadDataResult.Fail($"Import failed: {ex.Message}");
            }
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
        }
    }
}
