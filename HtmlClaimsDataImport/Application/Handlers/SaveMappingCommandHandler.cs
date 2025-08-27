namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Interfaces;
    using Mediator;

    public class SaveMappingCommandHandler(IConfigService configService) : ICommandHandler<SaveMappingCommand, bool>
    {
        private readonly IConfigService configService = configService;

        public async ValueTask<bool> Handle(SaveMappingCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.tmpDir) || string.IsNullOrWhiteSpace(request.outputColumn) || string.IsNullOrWhiteSpace(request.importColumn))
            {
                return false;
            }

            return await this.configService.SaveMappingAsync(request.tmpDir, request.outputColumn, request.importColumn, cancellationToken).ConfigureAwait(false);
        }
    }
}

