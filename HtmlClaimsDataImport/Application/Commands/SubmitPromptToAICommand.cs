namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record SubmitPromptToAICommand(string TmpDir, string PromptText) : ICommand<AIResponseDto>;
}
