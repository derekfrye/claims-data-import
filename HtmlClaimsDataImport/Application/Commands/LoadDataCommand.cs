using HtmlClaimsDataImport.Application.Commands.Results;
using Mediator;

namespace HtmlClaimsDataImport.Application.Commands
{
    public record LoadDataCommand(
        string TmpDir,
        string FileName,
        string JsonPath,
        string DatabasePath) : ICommand<LoadDataResult>;
}
