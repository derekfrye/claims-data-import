using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using Mediator;

namespace HtmlClaimsDataImport.Application.Handlers
{
    public class SubmitPromptToAICommandHandler(IAICompletionService aiService)
        : ICommandHandler<SubmitPromptToAICommand, AIResponseDto>
    {
        private readonly IAICompletionService aiService = aiService;

        public async ValueTask<AIResponseDto> Handle(SubmitPromptToAICommand request, CancellationToken cancellationToken)
        {
            return await aiService.CompleteAsync(request.TmpDir, request.PromptText, cancellationToken).ConfigureAwait(false);
        }
    }
}
