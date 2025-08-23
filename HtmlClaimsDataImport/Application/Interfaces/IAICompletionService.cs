namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Application.Queries.Dtos;

    public interface IAICompletionService
    {
        Task<AIResponseDto> CompleteAsync(string tmpdir, string prompt, CancellationToken cancellationToken = default);
    }
}
