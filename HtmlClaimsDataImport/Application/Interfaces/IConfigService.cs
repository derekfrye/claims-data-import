namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IConfigService
    {
        Task<bool> SaveMappingAsync(string tmpdir, string outputColumn, string importColumn, CancellationToken cancellationToken = default);
        Task<bool> ClearMappingAsync(string tmpdir, string outputColumn, CancellationToken cancellationToken = default);
        Task<byte[]> ReadConfigAsync(string tmpdir, CancellationToken cancellationToken = default);
    }
}
