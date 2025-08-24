namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record GetPreviewDataQuery(
        string tmpDir,
        int mappingStep = 0,
        string selectedColumn = "") : IQuery<PreviewDataDto>;
}
