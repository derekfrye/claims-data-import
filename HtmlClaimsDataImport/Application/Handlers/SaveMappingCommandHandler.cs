using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Interfaces;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class SaveMappingCommandHandler(IConfigService configService) : ICommandHandler<SaveMappingCommand, bool>
    {
        private readonly IConfigService configService = configService;

        public async ValueTask<bool> Handle(SaveMappingCommand request, CancellationToken cancellationToken)
        {
            return !string.IsNullOrWhiteSpace(request.TmpDir)
                && !string.IsNullOrWhiteSpace(request.OutputColumn)
                && !string.IsNullOrWhiteSpace(request.ImportColumn)
                && await this.configService.SaveMappingAsync(request.TmpDir, request.OutputColumn, request.ImportColumn, cancellationToken).ConfigureAwait(false);
        }
    }
}
