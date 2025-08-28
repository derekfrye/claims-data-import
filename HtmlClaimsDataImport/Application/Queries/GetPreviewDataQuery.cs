using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Queries
{
    public record GetPreviewDataQuery(
        string TmpDir,
        int MappingStep = 0,
        string SelectedColumn = "") : IQuery<PreviewDataDto>;
}
