namespace HtmlClaimsDataImport.Application.Commands
{
    using MediatR;

    public record LoadDataCommand(
        string tmpDir,
        string fileName,
        string jsonPath,
        string databasePath) : IRequest<string>;
}