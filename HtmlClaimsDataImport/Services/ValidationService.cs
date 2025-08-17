namespace HtmlClaimsDataImport.Services
{
    using Microsoft.Data.Sqlite;

    public static class ValidationService
    {
        public static (bool isValid, string errorMessage) ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return (false, "File path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                return (false, $"File not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return (false, "File is empty.");
            }

            return (true, string.Empty);
        }

        public static (bool isValid, string errorMessage) ValidateJsonFile(string jsonPath)
        {
            if (jsonPath == "default")
            {
                string defaultJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "default_config.json");
                return ValidateFile(defaultJsonPath);
            }
            
            var (isValid, errorMessage) = ValidateFile(jsonPath);
            if (!isValid)
            {
                return (isValid, errorMessage);
            }

            // Additional JSON-specific validation could go here
            try
            {
                var jsonContent = File.ReadAllText(jsonPath);
                System.Text.Json.JsonDocument.Parse(jsonContent);
                return (true, string.Empty);
            }
            catch (System.Text.Json.JsonException ex)
            {
                return (false, $"Invalid JSON format: {ex.Message}");
            }
        }

        public static (bool isValid, string errorMessage) ValidateSqliteDatabase(string databasePath)
        {
            var (isValid, errorMessage) = ValidateFile(databasePath);
            if (!isValid)
            {
                return (isValid, errorMessage);
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
                    return (false, "Database does not contain a 'claims' table.");
                }
                
                return (true, string.Empty);
            }
            catch (SqliteException ex)
            {
                return (false, $"Invalid SQLite database: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Database validation error: {ex.Message}");
            }
        }
    }
}