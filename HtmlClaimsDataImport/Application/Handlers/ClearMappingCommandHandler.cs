
using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Interfaces;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class ClearMappingCommandHandler(IConfigService configService) : ICommandHandler<ClearMappingCommand, bool>
    {
        private readonly IConfigService configService = configService;

        public async ValueTask<bool> Handle(ClearMappingCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TmpDir) || string.IsNullOrWhiteSpace(request.OutputColumn))
            {
                return false;
            }

            var ok = await configService.ClearMappingAsync(request.TmpDir, request.OutputColumn, cancellationToken).ConfigureAwait(false);
            return ok;
        }
    }
}
