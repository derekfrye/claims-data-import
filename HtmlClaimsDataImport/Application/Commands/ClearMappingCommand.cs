
using Mediator;

namespace HtmlClaimsDataImport.Application.Commands
{
    public record ClearMappingCommand(string TmpDir, string OutputColumn) : ICommand<bool>;
}
