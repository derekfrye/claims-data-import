namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record GetPreviewDataQuery(
        string TmpDir,
        int MappingStep = 0,
        string SelectedColumn = "") : IQuery<PreviewDataDto>;
}
