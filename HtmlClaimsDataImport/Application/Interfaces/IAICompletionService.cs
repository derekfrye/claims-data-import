using HtmlClaimsDataImport.Application.Queries.Dtos;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IAICompletionService
    {
        Task<AIResponseDto> CompleteAsync(string tmpdir, string prompt, CancellationToken cancellationToken = default);
    }
}
