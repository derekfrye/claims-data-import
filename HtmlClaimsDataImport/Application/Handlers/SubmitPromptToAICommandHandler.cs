namespace HtmlClaimsDataImport.Application.Handlers
{
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Application.Queries.Dtos;
    using Mediator;

    public class SubmitPromptToAICommandHandler(IAICompletionService aiService)
        : IRequestHandler<SubmitPromptToAICommand, AIResponseDto>
    {
        private readonly IAICompletionService aiService = aiService;

        public async ValueTask<AIResponseDto> Handle(SubmitPromptToAICommand request, CancellationToken cancellationToken)
        {
            return await this.aiService.CompleteAsync(request.tmpDir, request.promptText, cancellationToken).ConfigureAwait(false);
        }
    }
}
