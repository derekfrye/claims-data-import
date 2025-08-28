namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public class GetPreviewDataQueryHandler : IQueryHandler<GetPreviewDataQuery, PreviewDataDto>
    {
        private readonly IPreviewService previewService;

        public GetPreviewDataQueryHandler(IPreviewService previewService)
        {
            this.previewService = previewService;
        }

        public async ValueTask<PreviewDataDto> Handle(GetPreviewDataQuery request, CancellationToken cancellationToken)
        {
            return await this.previewService.GetPreviewDataAsync(request.TmpDir, request.MappingStep, request.SelectedColumn).ConfigureAwait(false);
        }
    }
}
