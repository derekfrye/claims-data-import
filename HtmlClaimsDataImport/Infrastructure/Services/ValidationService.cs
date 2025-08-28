namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Microsoft.Data.Sqlite;

    public class ValidationService : IValidationService
    {
        public Task<ValidationResult> ValidateFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(ValidationResult.Failure("File path cannot be empty."));
            }

            if (!File.Exists(filePath))
            {
                return Task.FromResult(ValidationResult.Failure($"File not found: {filePath}"));
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return Task.FromResult(ValidationResult.Failure("File is empty."));
            }

            return Task.FromResult(ValidationResult.Success());
        }

        public async Task<ValidationResult> ValidateJsonFileAsync(string jsonPath)
        {
            if (string.Equals(jsonPath, "default", StringComparison.OrdinalIgnoreCase))
            {
                string defaultJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "default_config.json");
                return await this.ValidateFileAsync(defaultJsonPath).ConfigureAwait(false);
            }
            
            var validationResult = await this.ValidateFileAsync(jsonPath).ConfigureAwait(false);
            if (!validationResult.isValid)
            {
                return validationResult;
            }

            // Additional JSON-specific validation could go here
            try
            {
                var jsonContent = await File.ReadAllTextAsync(jsonPath).ConfigureAwait(false);
                using var _ = System.Text.Json.JsonDocument.Parse(jsonContent);
                return ValidationResult.Success();
            }
            catch (System.Text.Json.JsonException ex)
            {
                return ValidationResult.Failure($"Invalid JSON format: {ex.Message}");
            }
        }

        public async Task<ValidationResult> ValidateSqliteDatabaseAsync(string databasePath)
        {
            var validationResult = await this.ValidateFileAsync(databasePath).ConfigureAwait(false);
            if (!validationResult.isValid)
            {
                return validationResult;
            }

            try
            {
                var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
                await connection.OpenAsync().ConfigureAwait(false);
                try
                {
                    var command = connection.CreateCommand();
                    try
                    {
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='claims';";
                        var result = await command.ExecuteScalarAsync().ConfigureAwait(false);

                        if (result == null)
                        {
                            return ValidationResult.Failure("Database does not contain a 'claims' table.");
                        }
                        
                        return ValidationResult.Success();
                    }
                    finally
                    {
                        await command.DisposeAsync().ConfigureAwait(false);
                    }
                }
                finally
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (SqliteException ex)
            {
                return ValidationResult.Failure($"Invalid SQLite database: {ex.Message}");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Database validation error: {ex.Message}");
            }
        }
    }
}
