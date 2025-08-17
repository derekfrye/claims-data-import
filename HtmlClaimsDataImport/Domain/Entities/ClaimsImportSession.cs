namespace HtmlClaimsDataImport.Domain.Entities
{
    public class ClaimsImportSession
    {
        public string TempDirectory { get; }
        public string? FileName { get; private set; }
        public string? JsonConfigPath { get; private set; }
        public string? DatabasePath { get; private set; }
        public string? ImportTableName { get; private set; }
        public DateTime CreatedAt { get; }

        public ClaimsImportSession(string tempDirectory)
        {
            this.TempDirectory = tempDirectory ?? throw new ArgumentNullException(nameof(tempDirectory));
            this.CreatedAt = DateTime.UtcNow;
        }

        public void SetFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            }

            this.FileName = fileName;
        }

        public void SetJsonConfig(string jsonConfigPath)
        {
            this.JsonConfigPath = jsonConfigPath;
        }

        public void SetDatabase(string databasePath)
        {
            this.DatabasePath = databasePath;
        }

        public void SetImportTable(string importTableName)
        {
            this.ImportTableName = importTableName;
        }

        public bool IsReadyForImport => !string.IsNullOrEmpty(this.FileName) && 
                                       !string.IsNullOrEmpty(this.DatabasePath);

        public bool HasImportedData => !string.IsNullOrEmpty(this.ImportTableName);
    }
}