using Mediator;

namespace HtmlClaimsDataImport.Application.Commands
{
    public record SaveMappingCommand(
        string TmpDir,
        string OutputColumn,
        string ImportColumn) : ICommand<bool>;
}
