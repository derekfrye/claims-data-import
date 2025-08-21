namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using MediatR;

    public record GetPreviewDataQuery(
        string tmpDir,
        int mappingStep = 0,
        string selectedColumn = "") : IRequest<PreviewDataDto>;
}
