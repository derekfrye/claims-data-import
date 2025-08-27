namespace HtmlClaimsDataImport.Application.Commands
{
    using Mediator;

    public record SaveMappingCommand(
        string tmpDir,
        string outputColumn,
        string importColumn) : ICommand<bool>;
}

