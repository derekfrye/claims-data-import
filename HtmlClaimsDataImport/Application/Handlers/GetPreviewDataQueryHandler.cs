namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using MediatR;

    public class GetPreviewDataQueryHandler : IRequestHandler<GetPreviewDataQuery, PreviewDataDto>
    {
        private readonly IPreviewService previewService;

        public GetPreviewDataQueryHandler(IPreviewService previewService)
        {
            this.previewService = previewService;
        }

        public async Task<PreviewDataDto> Handle(GetPreviewDataQuery request, CancellationToken cancellationToken)
        {
            return await this.previewService.GetPreviewDataAsync(request.tmpDir, request.mappingStep, request.selectedColumn).ConfigureAwait(false);
        }
    }
}
