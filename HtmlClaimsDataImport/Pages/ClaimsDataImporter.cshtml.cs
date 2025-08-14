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

        public IActionResult OnPostBrowseFile(string fileType)
        {
            // Simulate file browser dialog result
            // In a real implementation, this would integrate with browser file APIs
            // or use a custom file picker dialog
            
            var userAction = Request.Headers["X-User-Action"].ToString();
            
            if (userAction == "ok")
            {
                // Simulate user selected a file
                var simulatedPath = fileType switch
                {
                    "json" => "/path/to/config.json",
                    "filename" => "/path/to/data.csv",
                    "database" => "/path/to/claims.db",
                    _ => "/path/to/file"
                };
                
                // Update the corresponding property
                switch (fileType)
                {
                    case "json":
                        JsonFile = simulatedPath;
                        JsonFileStatus = "user pressed ok";
                        return Content(JsonFileStatus);
                    case "filename":
                        FileName = simulatedPath;
                        FileNameStatus = "user pressed ok";
                        return Content(FileNameStatus);
                    case "database":
                        Database = simulatedPath;
                        DatabaseStatus = "user pressed ok";
                        return Content(DatabaseStatus);
                }
            }
            else if (userAction == "cancel")
            {
                // User cancelled
                switch (fileType)
                {
                    case "json":
                        JsonFile = string.Empty;
                        JsonFileStatus = "user pressed cancel";
                        return Content(JsonFileStatus);
                    case "filename":
                        FileName = string.Empty;
                        FileNameStatus = "user pressed cancel";
                        return Content(FileNameStatus);
                    case "database":
                        Database = string.Empty;
                        DatabaseStatus = "user pressed cancel";
                        return Content(DatabaseStatus);
                }
            }
            
            // Default case - simulate OK for demonstration
            var defaultPath = fileType switch
            {
                "json" => "/selected/config.json",
                "filename" => "/selected/data.csv", 
                "database" => "/selected/claims.db",
                _ => "/selected/file"
            };
            
            switch (fileType)
            {
                case "json":
                    JsonFile = defaultPath;
                    JsonFileStatus = "user pressed ok";
                    return Content(JsonFileStatus);
                case "filename":
                    FileName = defaultPath;
                    FileNameStatus = "user pressed ok";
                    return Content(FileNameStatus);
                case "database":
                    Database = defaultPath;
                    DatabaseStatus = "user pressed ok";
                    return Content(DatabaseStatus);
            }
            
            return Content("user pressed ok");
        }
    }
}