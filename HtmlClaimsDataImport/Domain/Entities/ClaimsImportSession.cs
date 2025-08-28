namespace HtmlClaimsDataImport.Domain.Entities
{
    public class ClaimsImportSession(string tempDirectory)
    {
        public string TempDirectory { get; } = tempDirectory ?? throw new ArgumentNullException(nameof(tempDirectory));
        public string? FileName { get; private set; }
        public string? JsonConfigPath { get; private set; }
        public string? DatabasePath { get; private set; }
        public string? ImportTableName { get; private set; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public void SetFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            }

            FileName = fileName;
        }

        public void SetJsonConfig(string jsonConfigPath)
        {
            JsonConfigPath = jsonConfigPath;
        }

        public void SetDatabase(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public void SetImportTable(string importTableName)
        {
            ImportTableName = importTableName;
        }

        public bool IsReadyForImport => !string.IsNullOrEmpty(FileName) &&
                                       !string.IsNullOrEmpty(DatabasePath);

        public bool HasImportedData => !string.IsNullOrEmpty(ImportTableName);
    }
}