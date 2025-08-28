using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Queries
{
    public record GetMappingTranslationQuery(
        string TmpDir,
        int MappingStep,
        string SelectedColumn) : IQuery<MappingTranslationDto>;
}
