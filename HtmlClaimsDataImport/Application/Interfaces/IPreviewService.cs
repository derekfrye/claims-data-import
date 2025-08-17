namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Models;

    public interface IPreviewService
    {
        Task<PreviewDataModel> GetPreviewDataAsync(string tmpdir, int mappingStep, string selectedColumn);
    }
}