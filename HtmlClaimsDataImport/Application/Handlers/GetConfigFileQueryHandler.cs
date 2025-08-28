using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class GetConfigFileQueryHandler(IConfigService configService) : IQueryHandler<GetConfigFileQuery, GetConfigFileResult>
    {
        private readonly IConfigService configService = configService;

        public async ValueTask<GetConfigFileResult> Handle(GetConfigFileQuery request, CancellationToken cancellationToken)
        {
            var bytes = await configService.ReadConfigAsync(request.TmpDir, cancellationToken).ConfigureAwait(false);
            return new GetConfigFileResult
            {
                Content = bytes,
                ContentType = "application/json",
                FileName = "ClaimsDataImportConfig.json",
            };
        }
    }
}
