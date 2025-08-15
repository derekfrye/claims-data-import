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

        public void OnGet()
        {
            // Initialize page
        }

        public async Task<IActionResult> OnPostFileUpload(string fileType, IFormFile uploadedFile)
        {
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                return Content("No file selected");
            }

            // Create temp directory and save file
            var tempDir = TempDirectoryCleanupService.CreateTempDirectory();
            var fileName = Path.GetFileName(uploadedFile.FileName);
            var filePath = Path.Combine(tempDir, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(stream);
            }

            var statusMessage = $"File uploaded: {fileName}";
            
            // Update the corresponding property based on file type
            switch (fileType)
            {
                case "json":
                    JsonFile = filePath;
                    JsonFileStatus = statusMessage;
                    return Content($@"<span id=""json-status"" class=""file-status"">{JsonFileStatus}</span>
                                     <input type=""text"" id=""jsonFile"" name=""JsonFile"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />");
                case "filename":
                    FileName = filePath;
                    FileNameStatus = statusMessage;
                    return Content($@"<span id=""filename-status"" class=""file-status"">{FileNameStatus}</span>
                                     <input type=""text"" id=""fileName"" name=""FileName"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />");
                case "database":
                    Database = filePath;
                    DatabaseStatus = statusMessage;
                    return Content($@"<span id=""database-status"" class=""file-status"">{DatabaseStatus}</span>
                                     <input type=""text"" id=""database"" name=""Database"" value=""{filePath}"" readonly hx-swap-oob=""outerHTML"" />");
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
    }
}