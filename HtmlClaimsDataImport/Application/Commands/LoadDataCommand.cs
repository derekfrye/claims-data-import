namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Commands.Results;
    using Mediator;

    public record LoadDataCommand(
        string TmpDir,
        string FileName,
        string JsonPath,
        string DatabasePath) : ICommand<LoadDataResult>;
}
