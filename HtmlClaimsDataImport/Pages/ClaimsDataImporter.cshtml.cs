namespace HtmlClaimsDataImport.Pages
{
    using System;
    using System.IO;
    using HtmlClaimsDataImport.Models;
    using HtmlClaimsDataImport.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsDataImporter"/> class.
    /// </summary>
    /// <param name="tempDirectoryService">The service for managing temporary directories.</param>
    public class ClaimsDataImporter(ITempDirectoryService tempDirectoryService) : PageModel
    {
        private readonly ITempDirectoryService tempDirectoryService = tempDirectoryService;

        /// <summary>
        /// Gets or sets the path to the JSON file.
        /// </summary>
        [BindProperty]
        public string JsonFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        [BindProperty]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database connection string or identifier.
        /// </summary>
        [BindProperty]
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON file selection mode (default or upload).
        /// </summary>
        [BindProperty]
        public string JsonMode { get; set; } = "default";

        /// <summary>
        /// Gets or sets the database selection mode (default or upload).
        /// </summary>
        [BindProperty]
        public string DatabaseMode { get; set; } = "default";

        /// <summary>
        /// Gets or sets the status message for the JSON file.
        /// </summary>
        public string JsonFileStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status message for the file name.
        /// </summary>
        public string FileNameStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status message for the database.
        /// </summary>
        public string DatabaseStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets the temporary directory for the current session.
        /// </summary>
        public string TempDirectory => this.tempDirectoryService.GetSessionTempDirectory();

        /// <summary>
        /// Gets the default JSON configuration file name.
        /// </summary>
        public string DefaultJsonFile => "system.json";

        /// <summary>
        /// Gets the default database file name.
        /// </summary>
        public string DefaultDatabase => "claims.db";

        /// <summary>
        /// Handles GET requests to initialize the page.
        /// </summary>
        public void OnGet()
        {
            // Initialize default values if not already set
            if (string.IsNullOrEmpty(this.JsonFile) && this.JsonMode == "default")
            {
                this.JsonFile = this.DefaultJsonFile;
                this.JsonFileStatus = "Using default configuration";
            }

            if (string.IsNullOrEmpty(this.Database) && this.DatabaseMode == "default")
            {
                this.Database = this.DefaultDatabase;
                this.DatabaseStatus = "Using default database";
            }
        }

        /// <summary>
        /// Handles the file upload action for a specific file type.
        /// </summary>
        /// <param name="fileType">The type of the file being uploaded (e.g., json, filename, database).</param>
        /// <param name="uploadedFile">The uploaded file to process.</param>
        /// <param name="tmpdir">Optional temp directory to use. If not specified, uses session-based logic.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        public async Task<IActionResult> OnPostFileUpload(string fileType, IFormFile uploadedFile, string? tmpdir = null)
        {
            Console.WriteLine($"OnPostFileUpload called: fileType={fileType}, file={uploadedFile?.FileName}, size={uploadedFile?.Length}");

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                Console.WriteLine("No file uploaded");
                return this.Content("No file selected");
            }

            // Use specified temp directory if provided, otherwise use session temp directory
            string tempDir;
            if (!string.IsNullOrEmpty(tmpdir))
            {
                // Security validation: ensure tmpdir is within authorized base path
                var authorizedBasePath = Services.TempDirectoryCleanupService.GetTempBasePath();
                var normalizedTmpdir = Path.GetFullPath(tmpdir);
                var normalizedBasePath = Path.GetFullPath(authorizedBasePath);

                if (!normalizedTmpdir.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠️  SECURITY WARNING: tmpdir parameter '{tmpdir}' is outside authorized base path '{authorizedBasePath}'. Reverting to session-based logic.");
                    tempDir = this.tempDirectoryService.GetSessionTempDirectory();
                }
                else
                {
                    tempDir = tmpdir;
                    // Ensure the directory exists if tmpdir was specified and validated
                    if (!Directory.Exists(tmpdir))
                    {
                        Directory.CreateDirectory(tmpdir);
                    }
                }
            }
            else
            {
                tempDir = this.tempDirectoryService.GetSessionTempDirectory();
            }

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

            // Update the corresponding property and create model for partial view
            var model = new FileUploadResponseModel
            {
                FileType = fileType,
                StatusMessage = statusMessage,
                FilePath = filePath,
                LogEntry = logEntry,
            };

            switch (fileType)
            {
                case "json":
                    this.JsonFile = filePath;
                    this.JsonFileStatus = statusMessage;
                    model.InputId = "jsonFile";
                    model.InputName = "JsonFile";
                    break;
                case "filename":
                    this.FileName = filePath;
                    this.FileNameStatus = statusMessage;
                    model.InputId = "fileName";
                    model.InputName = "FileName";
                    break;
                case "database":
                    this.Database = filePath;
                    this.DatabaseStatus = statusMessage;
                    model.InputId = "database";
                    model.InputName = "Database";
                    break;
            }

            var partialView = await this.RenderPartialViewAsync("_FileUploadResponse", model);
            return this.Content(partialView, "text/html");
        }

        /// <summary>
        /// Handles the file selection action for a specific file type.
        /// </summary>
        /// <param name="fileType">The type of the file (e.g., json, filename, database).</param>
        /// <param name="fileName">The name of the selected file.</param>
        /// <param name="action">The action performed (e.g., ok or cancel).</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        public async Task<IActionResult> OnPostFileSelected(string fileType, string fileName, string action)
        {
            var statusMessage = action == "ok" ? "user pressed ok" : "user pressed cancel";

            // Update the corresponding property based on file type
            switch (fileType)
            {
                case "json":
                    this.JsonFile = action == "ok" ? fileName : string.Empty;
                    this.JsonFileStatus = statusMessage;
                    return this.Content(this.JsonFileStatus);
                case "filename":
                    this.FileName = action == "ok" ? fileName : string.Empty;
                    this.FileNameStatus = statusMessage;
                    var fileSelectedModel = new FileSelectedResponseModel
                    {
                        FileType = fileType,
                        StatusMessage = this.FileNameStatus,
                        FileName = this.FileName,
                        InputId = "fileName",
                        InputName = "FileName",
                    };
                    var partialView = await this.RenderPartialViewAsync("_FileSelectedResponse", fileSelectedModel);
                    return this.Content(partialView, "text/html");
                case "database":
                    this.Database = action == "ok" ? fileName : string.Empty;
                    this.DatabaseStatus = statusMessage;
                    return this.Content(this.DatabaseStatus);
            }

            return this.Content(statusMessage);
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

        private async Task<string> RenderPartialViewAsync<T>(string partialName, T model)
        {
            var viewEngine = this.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine>();
            var viewResult = viewEngine.FindView(this.PageContext, partialName, false);

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Partial view '{partialName}' not found");
            }

            var viewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<T>(this.ViewData, model);

            using var writer = new StringWriter();
            var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                this.PageContext,
                viewResult.View,
                viewData,
                this.TempData,
                writer,
                new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }
    }
}