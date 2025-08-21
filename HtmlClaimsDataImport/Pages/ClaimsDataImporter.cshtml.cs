namespace HtmlClaimsDataImport.Pages
{
    using System;
    using System.IO;
    using HtmlClaimsDataImport.Application.Commands;
    using HtmlClaimsDataImport.Application.Queries;
    using HtmlClaimsDataImport.Infrastructure.Services;
    using HtmlClaimsDataImport.Models;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimsDataImporter"/> class.
    /// </summary>
    /// <param name="tempDirectoryService">The service for managing temporary directories.</param>
    /// <param name="mediator">The mediator for handling commands and queries.</param>
    public class ClaimsDataImporter(ITempDirectoryService tempDirectoryService, IMediator mediator) : PageModel
    {
        private readonly ITempDirectoryService tempDirectoryService = tempDirectoryService;
        private readonly IMediator mediator = mediator;

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
            if (string.IsNullOrEmpty(this.JsonFile) && string.Equals(this.JsonMode, "default", StringComparison.OrdinalIgnoreCase))
            {
                this.JsonFile = this.DefaultJsonFile;
                this.JsonFileStatus = "Using default configuration";
            }

            if (string.IsNullOrEmpty(this.Database) && string.Equals(this.DatabaseMode, "default", StringComparison.OrdinalIgnoreCase))
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
            var command = new UploadFileCommand(fileType, uploadedFile, tmpdir);
            var result = await this.mediator.Send(command);

            // Update the corresponding property and create model for partial view
            var model = new FileUploadResponseModel
            {
                FileType = fileType,
                StatusMessage = result.statusMessage,
                FilePath = result.filePath,
                LogEntry = result.logEntry,
            };

            switch (fileType)
            {
                case "json":
                    this.JsonFile = result.filePath;
                    this.JsonFileStatus = result.statusMessage;
                    model.InputId = "jsonFile";
                    model.InputName = "JsonFile";
                    break;
                case "filename":
                    this.FileName = result.filePath;
                    this.FileNameStatus = result.statusMessage;
                    model.InputId = "fileName";
                    model.InputName = "FileName";
                    break;
                case "database":
                    this.Database = result.filePath;
                    this.DatabaseStatus = result.statusMessage;
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
            var statusMessage = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? "user pressed ok" : "user pressed cancel";

            // Update the corresponding property based on file type
            switch (fileType)
            {
                case "json":
                    this.JsonFile = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
                    this.JsonFileStatus = statusMessage;
                    return this.Content(this.JsonFileStatus);
                case "filename":
                    this.FileName = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
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
                    this.Database = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
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
                var query = new GetPreviewDataQuery(tmpdir, mappingStep, selectedColumn);
                var previewModel = await this.mediator.Send(query);
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
            var command = new LoadDataCommand(tmpdir, fileName, jsonPath, databasePath);
            var result = await this.mediator.Send(command);
            return this.Content(result);
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
