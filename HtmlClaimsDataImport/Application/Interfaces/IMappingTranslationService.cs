using HtmlClaimsDataImport.Application.Queries.Dtos;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IMappingTranslationService
    {
        Task<MappingTranslationDto> BuildPromptAsync(string tmpdir, int mappingStep, string selectedColumn);
    }
}

