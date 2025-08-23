namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Commands.Results;
    using MediatR;

    public record LoadDataCommand(
        string tmpDir,
        string fileName,
        string jsonPath,
        string databasePath) : IRequest<LoadDataResult>;
}
