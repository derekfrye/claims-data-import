namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record GetMappingTranslationQuery(
        string TmpDir,
        int MappingStep,
        string SelectedColumn) : IQuery<MappingTranslationDto>;
}
