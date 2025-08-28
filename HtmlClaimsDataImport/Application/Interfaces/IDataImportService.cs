namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Application.Commands.Results;

    public interface IDataImportService
    {
        Task<string> ResolveActualPath(string path, string tmpdir, string defaultFileName);
    Task<LoadDataResult> ProcessFileImport(string fileName, string jsonPath, string databasePath);
    }
}
