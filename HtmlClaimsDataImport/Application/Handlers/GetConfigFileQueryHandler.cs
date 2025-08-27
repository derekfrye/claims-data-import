namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public class GetConfigFileQueryHandler(IConfigService configService) : IQueryHandler<GetConfigFileQuery, GetConfigFileResult>
    {
        private readonly IConfigService configService = configService;

        public async ValueTask<GetConfigFileResult> Handle(GetConfigFileQuery request, CancellationToken cancellationToken)
        {
            var bytes = await this.configService.ReadConfigAsync(request.tmpDir, cancellationToken).ConfigureAwait(false);
            return new GetConfigFileResult
            {
                Content = bytes,
                ContentType = "application/json",
                FileName = "ClaimsDataImportConfig.json",
            };
        }
    }
}

