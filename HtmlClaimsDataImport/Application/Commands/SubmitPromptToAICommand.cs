namespace HtmlClaimsDataImport.Application.Commands
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using MediatR;

    public record SubmitPromptToAICommand(string tmpDir, string promptText) : IRequest<AIResponseDto>;
}

