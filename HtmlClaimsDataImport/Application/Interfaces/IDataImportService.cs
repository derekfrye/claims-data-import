namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IDataImportService
    {
        Task<string> ResolveActualPath(string path, string tmpdir, string defaultFileName);
        Task<string> ProcessFileImport(string fileName, string jsonPath, string databasePath);
    }
}