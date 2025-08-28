using HtmlClaimsDataImport.Application.Queries.Dtos;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IPreviewService
    {
        Task<PreviewDataDto> GetPreviewDataAsync(string tmpdir, int mappingStep, string selectedColumn);
    }
}
