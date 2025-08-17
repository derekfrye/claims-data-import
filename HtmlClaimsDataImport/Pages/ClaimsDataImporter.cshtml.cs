namespace HtmlClaimsDataImport.Pages
{
    using System;
    using System.IO;
    using System.Text.Json;
    using HtmlClaimsDataImport.Models;
    using HtmlClaimsDataImport.Services;
    using LibClaimsDataImport.Importer;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Data.Sqlite;
    using Sylvan.Data.Csv;

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
        public string DefaultJsonFile => "default.json";

        /// <summary>
        /// Gets the default database file name.
        /// </summary>
        public string DefaultDatabase => "default.sqlite3.db";

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

            // Use service for file upload
            var (statusMessage, logEntry, filePath) = await FileUploadService.HandleFileUploadAsync(uploadedFile, fileType, tempDir);

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

        /// <summary>
        /// Handles the preview data action to show column mapping interface.
        /// </summary>
        /// <param name="tmpdir">The temporary directory path.</param>
        /// <param name="mappingStep">The current mapping step (optional).</param>
        /// <param name="selectedColumn">The selected import column (optional).</param>
        /// <returns>An <see cref="IActionResult"/> representing the preview data.</returns>
        public async Task<IActionResult> OnPostPreview(string tmpdir, int mappingStep = 0, string selectedColumn = "")
        {
            try
            {
                var previewModel = await PreviewService.GetPreviewDataAsync(tmpdir, mappingStep, selectedColumn);
                var partialView = await this.RenderPartialViewAsync("_PreviewContent", previewModel);
                return this.Content(partialView, "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Preview: {ex.Message}");
                var errorModel = new PreviewDataModel
                {
                    StatusMessage = $"Error: {ex.Message}",
                    IsPreviewAvailable = false,
                };
                var partialView = await this.RenderPartialViewAsync("_PreviewContent", errorModel);
                return this.Content(partialView, "text/html");
            }
        }

        /// <summary>
        /// Handles the load data action.
        /// </summary>
        /// <param name="tmpdir">The temporary directory path.</param>
        /// <param name="fileName">The file name to load.</param>
        /// <param name="jsonPath">The JSON configuration path.</param>
        /// <param name="databasePath">The database path.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.</returns>
        public async Task<IActionResult> OnPostLoadData(string tmpdir, string fileName, string jsonPath, string databasePath)
        {
            try
            {
                // Resolve paths
                string actualJsonPath = this.ResolveJsonPath(jsonPath, tmpdir);
                string actualDatabasePath = await DataImportService.ResolveActualPath(databasePath, tmpdir, this.DefaultDatabase);
                string actualFileName = Path.Combine(tmpdir, fileName);

                // Validation step 1: Check if JSON is valid
                var (jsonValid, jsonError) = ValidationService.ValidateJsonFile(actualJsonPath);
                if (!jsonValid)
                {
                    return this.Content($"json invalid: {jsonError}");
                }

                // Validation step 2: Check if file exists and is readable
                var (fileValid, fileError) = ValidationService.ValidateFile(actualFileName);
                if (!fileValid)
                {
                    return this.Content(fileError);
                }

                // Validation step 3: Check if database exists and is readable as SQLite
                var (dbValid, dbError) = ValidationService.ValidateSqliteDatabase(actualDatabasePath);
                if (!dbValid)
                {
                    return this.Content(dbError);
                }

                // All validations passed - proceed with import
                var result = await DataImportService.ProcessFileImport(actualFileName, actualJsonPath, actualDatabasePath);
                return this.Content(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadData: {ex.Message}");
                return this.Content($"Error: {ex.Message}");
            }
        }

        private string ResolveJsonPath(string path, string tmpdir)
        {
            if (path == "default")
            {
                return Path.Combine(Directory.GetCurrentDirectory(), this.DefaultJsonFile);
            }

            return Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
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
