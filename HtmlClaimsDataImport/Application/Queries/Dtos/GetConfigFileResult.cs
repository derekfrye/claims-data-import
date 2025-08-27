namespace HtmlClaimsDataImport.Application.Queries.Dtos
{
    public sealed class GetConfigFileResult
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = "application/json";
        public string FileName { get; init; } = "ClaimsDataImportConfig.json";
    }
}

