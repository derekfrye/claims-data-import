namespace HtmlClaimsDataImport.Application.Handlers;

using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Interfaces;
using Mediator;

public class ClearMappingCommandHandler(IConfigService configService) : ICommandHandler<ClearMappingCommand, bool>
{
    private readonly IConfigService configService = configService;

    public async ValueTask<bool> Handle(ClearMappingCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.tmpDir) || string.IsNullOrWhiteSpace(request.outputColumn))
        {
            return false;
        }

        var ok = await this.configService.ClearMappingAsync(request.tmpDir, request.outputColumn, cancellationToken).ConfigureAwait(false);
        return ok;
    }
}
