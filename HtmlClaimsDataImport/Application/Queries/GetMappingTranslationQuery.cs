namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record GetMappingTranslationQuery(
        string tmpDir,
        int mappingStep,
        string selectedColumn) : IQuery<MappingTranslationDto>;
}
