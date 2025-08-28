using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Queries
{
    public record GetConfigFileQuery(string TmpDir) : IQuery<GetConfigFileResult>;
}
