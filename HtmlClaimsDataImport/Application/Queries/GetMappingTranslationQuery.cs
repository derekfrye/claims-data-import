namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using MediatR;

    public record GetMappingTranslationQuery(
        string tmpDir,
        int mappingStep,
        string selectedColumn) : IRequest<MappingTranslationDto>;
}

