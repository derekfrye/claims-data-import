namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;

    public interface IPreviewService
    {
        Task<PreviewDataDto> GetPreviewDataAsync(string tmpdir, int mappingStep, string selectedColumn);
    }
}
