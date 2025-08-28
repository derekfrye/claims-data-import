namespace HtmlClaimsDataImport.Application.Queries
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record GetConfigFileQuery(string TmpDir) : IQuery<GetConfigFileResult>;
}
