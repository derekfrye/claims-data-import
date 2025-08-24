namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Commands.Results;
    using Mediator;

    public record LoadDataCommand(
        string tmpDir,
        string fileName,
        string jsonPath,
        string databasePath) : ICommand<LoadDataResult>;
}
