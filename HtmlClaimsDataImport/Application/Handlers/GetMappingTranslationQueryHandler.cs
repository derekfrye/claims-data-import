using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class GetMappingTranslationQueryHandler(IMappingTranslationService mappingService)
        : IQueryHandler<GetMappingTranslationQuery, MappingTranslationDto>
    {
        private readonly IMappingTranslationService mappingService = mappingService;

        public async ValueTask<MappingTranslationDto> Handle(GetMappingTranslationQuery request, CancellationToken cancellationToken)
        {
            return await mappingService.BuildPromptAsync(request.TmpDir, request.MappingStep, request.SelectedColumn).ConfigureAwait(false);
        }
    }
}
