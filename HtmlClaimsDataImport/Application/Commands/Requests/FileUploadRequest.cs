namespace HtmlClaimsDataImport.Application.Commands.Requests
{
    public sealed class FileUploadRequest(Stream content, string fileName, long length, string contentType)
    {
        public Stream Content { get; } = content;
        public string FileName { get; } = fileName;
        public long Length { get; } = length;
        public string ContentType { get; } = contentType;
    }
}

