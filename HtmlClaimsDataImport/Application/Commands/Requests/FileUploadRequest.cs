namespace HtmlClaimsDataImport.Application.Commands.Requests
{
    public sealed class FileUploadRequest
    {
        public FileUploadRequest(Stream content, string fileName, long length, string contentType)
        {
            this.Content = content;
            this.FileName = fileName;
            this.Length = length;
            this.ContentType = contentType;
        }

        public Stream Content { get; }
        public string FileName { get; }
        public long Length { get; }
        public string ContentType { get; }
    }
}

