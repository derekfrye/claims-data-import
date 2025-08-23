namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;

    public interface IMappingTranslationService
    {
        Task<MappingTranslationDto> BuildPromptAsync(string tmpdir, int mappingStep, string selectedColumn);
    }
}

