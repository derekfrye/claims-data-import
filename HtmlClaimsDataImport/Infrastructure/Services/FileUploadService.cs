using HtmlClaimsDataImport.Application.Commands.Requests;
using HtmlClaimsDataImport.Application.Interfaces;
using HtmlClaimsDataImport.Domain.ValueObjects;

namespace HtmlClaimsDataImport.Infrastructure.Services
{
    public class FileUploadService : IFileUploadService
    {
        public string FormatFileSize(long bytes)
        {
            string[] suffixes = ["B", "KiB", "MiB", "GiB", "TiB"];
            var suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        public async Task<FileUploadResult> HandleFileUploadAsync(FileUploadRequest file, string fileType, string tmpdir)
        {
            if (file == null || file.Length == 0)
            {
                return new FileUploadResult("No file selected.", "", "");
            }

            // Ensure temp directory exists
            if (!Directory.Exists(tmpdir))
            {
                _ = Directory.CreateDirectory(tmpdir);
            }

            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(tmpdir, fileName);

            Console.WriteLine($"OnPostFileUpload called: fileType={fileType}, file={fileName}, size={file.Length}");

            if (file.Content.CanSeek)
            {
                file.Content.Position = 0;
            }
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.Content.CopyToAsync(stream).ConfigureAwait(false);
            }

            Console.WriteLine($"File saved to: {filePath}, exists: {File.Exists(filePath)}");

            var fileSize = new FileInfo(filePath).Length;
            var formattedSize = FormatFileSize(fileSize);
            var statusMessage = $"File uploaded: {fileName}";
            var logEntry = $"File uploaded: {fileName}, {formattedSize}";

            Console.WriteLine($"Log entry: {logEntry}");

            return new FileUploadResult(statusMessage, logEntry, filePath);
        }

        // HTML generation removed from service to keep UI concerns in Razor
    }
}
