using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
                    return Content(FileNameStatus);
                case "database":
                    Database = action == "ok" ? fileName : string.Empty;
                    DatabaseStatus = statusMessage;
                    return Content(DatabaseStatus);
            }
            
            return Content(statusMessage);
        }
    }
}