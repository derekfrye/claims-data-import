namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public record SubmitPromptToAICommand(string tmpDir, string promptText) : IRequest<AIResponseDto>;
}
