using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HtmlClaimsDataImport.Services;

namespace HtmlClaimsDataImport.Pages
{
    public class ClaimsDataImporterModel : PageModel
    {
        [BindProperty]
        public string JsonFile { get; set; } = string.Empty;
        
        [BindProperty]
        public string FileName { get; set; } = string.Empty;
        
        [BindProperty]
        public string Database { get; set; } = string.Empty;
        
        public string JsonFileStatus { get; set; } = string.Empty;
        public string FileNameStatus { get; set; } = string.Empty;
        public string DatabaseStatus { get; set; } = string.Empty;
        
        public string TempDirectory => TempDirectoryCleanupService.GetSessionTempDirectory();

        public void OnGet()
        {
            // Initialize page
        }

        public async Task<IActionResult> OnPostFileUpload(string fileType, IFormFile uploadedFile)
        {
            Console.WriteLine($"OnPostFileUpload called: fileType={fileType}, file={uploadedFile?.FileName}, size={uploadedFile?.Length}");
            
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                Console.WriteLine("No file uploaded");
                return Content("No file selected");
            }

            // Use the session temp directory and save file
            var tempDir = TempDirectoryCleanupService.GetSessionTempDirectory();
            var fileName = Path.GetFileName(uploadedFile.FileName);
            var filePath = Path.Combine(tempDir, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(stream);
            }
            
            Console.WriteLine($"File saved to: {filePath}, exists: {System.IO.File.Exists(filePath)}");

            var statusMessage = $"File uploaded: {fileName}";
            var formattedSize = FormatFileSize(uploadedFile.Length);
            var logEntry = $"File uploaded: {fileName}, {formattedSize}";
            
            Console.WriteLine($"Log entry: {logEntry}");
            
            // Update the corresponding property based on file type
            switch (fileType)
            {
                case "json":
                    JsonFile = filePath;
                    JsonFileStatus = statusMessage;
                    return Content($@"<span id=""json-status"" class=""file-status"">{JsonFileStatus}</span>
                                     <input type=""text"" id=""jsonFile"" name=""JsonFile"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />
                                     <div hx-swap-oob=""afterbegin:#upload-log"">{logEntry}<br/></div>");
                case "filename":
                    FileName = filePath;
                    FileNameStatus = statusMessage;
                    return Content($@"<span id=""filename-status"" class=""file-status"">{FileNameStatus}</span>
                                     <input type=""text"" id=""fileName"" name=""FileName"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />
                                     <div hx-swap-oob=""afterbegin:#upload-log"">{logEntry}<br/></div>");
                case "database":
                    Database = filePath;
                    DatabaseStatus = statusMessage;
                    return Content($@"<span id=""database-status"" class=""file-status"">{DatabaseStatus}</span>
                                     <input type=""text"" id=""database"" name=""Database"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />
                                     <div hx-swap-oob=""afterbegin:#upload-log"">{logEntry}<br/></div>");
            }
            
            return Content(statusMessage);
        }

        public IActionResult OnPostFileSelected(string fileType, string fileName, string action)
        {
            var statusMessage = action == "ok" ? "user pressed ok" : "user pressed cancel";
            
            // Update the corresponding property based on file type
            switch (fileType)
            {
                case "json":
                    JsonFile = action == "ok" ? fileName : string.Empty;
                    JsonFileStatus = statusMessage;
                    return Content(JsonFileStatus);
                case "filename":
                    FileName = action == "ok" ? fileName : string.Empty;
                    FileNameStatus = statusMessage;
                    return Content($@"<span id=""filename-status"" class=""file-status"">{FileNameStatus}</span>
                                     <input type=""text"" id=""fileName"" name=""FileName"" value=""{FileName}"" readonly hx-swap-oob=""outerHTML"" />");
                case "database":
                    Database = action == "ok" ? fileName : string.Empty;
                    DatabaseStatus = statusMessage;
                    return Content(DatabaseStatus);
            }
            
            return Content(statusMessage);
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KiB", "MiB", "GiB", "TiB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:0.##} {suffixes[suffixIndex]}";
        }
    }
}