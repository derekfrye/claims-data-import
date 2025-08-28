using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class GetPreviewDataQueryHandler(IPreviewService previewService) : IQueryHandler<GetPreviewDataQuery, PreviewDataDto>
    {
        private readonly IPreviewService previewService = previewService;

        public async ValueTask<PreviewDataDto> Handle(GetPreviewDataQuery request, CancellationToken cancellationToken)
        {
            return await previewService.GetPreviewDataAsync(request.TmpDir, request.MappingStep, request.SelectedColumn).ConfigureAwait(false);
        }
    }
}
