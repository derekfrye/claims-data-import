using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Commands
{
    public record SubmitPromptToAICommand(string TmpDir, string PromptText) : ICommand<AIResponseDto>;
}
