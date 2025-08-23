namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using MediatR;

    public class GetMappingTranslationQueryHandler(IMappingTranslationService mappingService)
        : IRequestHandler<GetMappingTranslationQuery, MappingTranslationDto>
    {
        private readonly IMappingTranslationService mappingService = mappingService;

        public async Task<MappingTranslationDto> Handle(GetMappingTranslationQuery request, CancellationToken cancellationToken)
        {
            return await this.mappingService.BuildPromptAsync(request.tmpDir, request.mappingStep, request.selectedColumn).ConfigureAwait(false);
        }
    }
}

