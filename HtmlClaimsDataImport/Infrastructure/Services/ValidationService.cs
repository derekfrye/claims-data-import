namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Domain.ValueObjects;
    using Microsoft.Data.Sqlite;

    public class ValidationService : IValidationService
    {
        public ValidationResult ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ValidationResult.Failure("File path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                return ValidationResult.Failure($"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return ValidationResult.Failure("File is empty.");
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateJsonFile(string jsonPath)
        {
            if (string.Equals(jsonPath, "default", StringComparison.OrdinalIgnoreCase))
            {
                string defaultJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "default_config.json");
                return this.ValidateFile(defaultJsonPath);
            }
            
            var validationResult = this.ValidateFile(jsonPath);
            if (!validationResult.isValid)
            {
                return validationResult;
            }

            // Additional JSON-specific validation could go here
            try
            {
                var jsonContent = File.ReadAllText(jsonPath);
                System.Text.Json.JsonDocument.Parse(jsonContent);
                return ValidationResult.Success();
            }
            catch (System.Text.Json.JsonException ex)
            {
                return ValidationResult.Failure($"Invalid JSON format: {ex.Message}");
            }
        }

        public ValidationResult ValidateSqliteDatabase(string databasePath)
        {
            var validationResult = this.ValidateFile(databasePath);
            if (!validationResult.isValid)
            {
                return validationResult;
            }

            try
            {
                using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='claims';";
                var result = command.ExecuteScalar();
                
                if (result == null)
                {
                    return ValidationResult.Failure("Database does not contain a 'claims' table.");
                }
                
                return ValidationResult.Success();
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
