namespace HtmlClaimsDataImport.Infrastructure.Services
{
    using HtmlClaimsDataImport.Application.Interfaces;
    using HtmlClaimsDataImport.Domain.ValueObjects;

    public class FileUploadService : IFileUploadService
    {
        public string FormatFileSize(long bytes)
    {
        string[] suffixes = ["B", "KiB", "MiB", "GiB", "TiB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }

        public async Task<FileUploadResult> HandleFileUploadAsync(IFormFile uploadedFile, string fileType, string tmpdir)
    {
        if (uploadedFile == null || uploadedFile.Length == 0)
        {
            return new FileUploadResult("No file selected.", "", "");
        }

        // Ensure temp directory exists
        if (!Directory.Exists(tmpdir))
        {
            Directory.CreateDirectory(tmpdir);
        }

        var fileName = Path.GetFileName(uploadedFile.FileName);
        var filePath = Path.Combine(tmpdir, fileName);

        Console.WriteLine($"OnPostFileUpload called: fileType={fileType}, file={fileName}, size={uploadedFile.Length}");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await uploadedFile.CopyToAsync(stream).ConfigureAwait(false);
        }

        Console.WriteLine($"File saved to: {filePath}, exists: {File.Exists(filePath)}");

        var fileSize = new FileInfo(filePath).Length;
        var formattedSize = this.FormatFileSize(fileSize);
        var statusMessage = $"File uploaded: {fileName}";
        var logEntry = $"File uploaded: {fileName}, {formattedSize}";

        Console.WriteLine($"Log entry: {logEntry}");

        return new FileUploadResult(statusMessage, logEntry, filePath);
    }

        public string GenerateFileStatusResponse(string fileType, string fileName, string action, string tmpdir, string status)
    {
        if (action == "cancel")
        {
            return $"<span id=\"{fileType}-status\" class=\"file-status\">No file selected</span>";
        }

        var logMessage = status;
        var filePath = string.IsNullOrEmpty(fileName) ? "" : Path.Combine(tmpdir, fileName);

        return $@"<span id=""{fileType}-status"" class=""file-status"" data-status=""{status}"">{status}</span>
<input type=""text"" 
       id=""{GetInputId(fileType)}"" 
       name=""{GetPropertyName(fileType)}"" 
       value=""{filePath}"" 
       readonly 
       hx-swap-oob=""outerHTML"" 
       data-file-path=""{filePath}"" />
<div hx-swap-oob=""afterbegin:#upload-log"" data-log-entry=""{logMessage}"">
    {logMessage}<br/>
</div>";
    }

        private static string GetInputId(string fileType) => fileType switch
    {
        "json" => "jsonFile",
        "filename" => "fileName",
        "database" => "database",
        _ => throw new ArgumentException($"Unknown file type: {fileType}")
    };

        private static string GetPropertyName(string fileType) => fileType switch
    {
        "json" => "JsonFile",
        "filename" => "FileName",
        "database" => "Database",
        _ => throw new ArgumentException($"Unknown file type: {fileType}")
    };
    }
}