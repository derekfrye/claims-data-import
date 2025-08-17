namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Models;
    using MediatR;

    public record GetPreviewDataQuery(
        string tmpDir,
        int mappingStep = 0,
        string selectedColumn = "") : IRequest<PreviewDataModel>;
}