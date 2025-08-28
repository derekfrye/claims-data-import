namespace HtmlClaimsDataImport.Application.Commands
{
    using Mediator;

    public record SaveMappingCommand(
        string TmpDir,
        string OutputColumn,
        string ImportColumn) : ICommand<bool>;
}
