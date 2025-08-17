namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Models;
    using MediatR;

    public class GetPreviewDataQueryHandler : IRequestHandler<GetPreviewDataQuery, PreviewDataModel>
    {
        private readonly IPreviewService previewService;

        public GetPreviewDataQueryHandler(IPreviewService previewService)
        {
            this.previewService = previewService;
        }

        public async Task<PreviewDataModel> Handle(GetPreviewDataQuery request, CancellationToken cancellationToken)
        {
            return await this.previewService.GetPreviewDataAsync(request.tmpDir, request.mappingStep, request.selectedColumn);
        }
    }
}