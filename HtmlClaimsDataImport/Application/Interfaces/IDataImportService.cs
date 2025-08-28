using HtmlClaimsDataImport.Application.Commands.Results;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IDataImportService
    {
        Task<string> ResolveActualPath(string path, string tmpdir, string defaultFileName);
        Task<LoadDataResult> ProcessFileImport(string fileName, string jsonPath, string databasePath);
    }
}
