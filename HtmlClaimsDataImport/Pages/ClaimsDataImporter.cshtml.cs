using HtmlClaimsDataImport.Application.Commands;
using HtmlClaimsDataImport.Application.Commands.Requests;
using HtmlClaimsDataImport.Application.Queries;
using HtmlClaimsDataImport.Models;
using HtmlClaimsDataImport.Application.Queries.Dtos;
using HtmlClaimsDataImport.Application.Interfaces;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmlClaimsDataImport.Pages
{
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
        public string TempDirectory => tempDirectoryService.GetSessionTempDirectory();

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
            if (string.IsNullOrEmpty(JsonFile) && string.Equals(JsonMode, "default", StringComparison.OrdinalIgnoreCase))
            {
                JsonFile = DefaultJsonFile;
                JsonFileStatus = "Using default configuration";
            }

            if (string.IsNullOrEmpty(Database) && string.Equals(DatabaseMode, "default", StringComparison.OrdinalIgnoreCase))
            {
                Database = DefaultDatabase;
                DatabaseStatus = "Using default database";
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
            // Handle missing/empty file without hitting mediator
            Domain.ValueObjects.FileUploadResult result;
            if (uploadedFile is null || uploadedFile.Length == 0)
            {
                result = new Domain.ValueObjects.FileUploadResult("No file selected", string.Empty, string.Empty);
            }
            else
            {
                // Adapt ASP.NET upload into application-agnostic request
                await using Stream content = uploadedFile.OpenReadStream();
                var req = new FileUploadRequest(content, Path.GetFileName(uploadedFile.FileName), uploadedFile.Length, uploadedFile.ContentType ?? string.Empty);
                var command = new UploadFileCommand(fileType, req, tmpdir);
                result = await mediator.Send(command, HttpContext.RequestAborted);
            }

            // Update the corresponding property and create model for partial view
            FileUploadResponseModel model = new()
            {
                FileType = fileType,
                StatusMessage = result.StatusMessage,
                FilePath = result.FilePath,
                LogEntry = result.LogEntry,
            };

            switch (fileType)
            {
                case "json":
                    JsonFile = result.FilePath;
                    JsonFileStatus = result.StatusMessage;
                    model.InputId = "jsonFile";
                    model.InputName = "JsonFile";
                    break;
                case "filename":
                    FileName = result.FilePath;
                    FileNameStatus = result.StatusMessage;
                    model.InputId = "fileName";
                    model.InputName = "FileName";
                    break;
                case "database":
                    Database = result.FilePath;
                    DatabaseStatus = result.StatusMessage;
                    model.InputId = "database";
                    model.InputName = "Database";
                    break;
                default:
                    // no-op for unknown file types
                    break;
            }

            var partialView = await RenderPartialViewAsync("_FileUploadResponse", model);
            return Content(partialView, "text/html");
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
                    JsonFile = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
                    JsonFileStatus = statusMessage;
                    return Content(JsonFileStatus);
                case "filename":
                    FileName = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
                    FileNameStatus = statusMessage;
                    var fileSelectedModel = new FileSelectedResponseModel
                    {
                        FileType = fileType,
                        StatusMessage = FileNameStatus,
                        FileName = FileName,
                        InputId = "fileName",
                        InputName = "FileName",
                    };
                    var partialView = await RenderPartialViewAsync("_FileSelectedResponse", fileSelectedModel);
                    return Content(partialView, "text/html");
                case "database":
                    Database = string.Equals(action, "ok", StringComparison.OrdinalIgnoreCase) ? fileName : string.Empty;
                    DatabaseStatus = statusMessage;
                    return Content(DatabaseStatus);
                default:
                    return Content(statusMessage);
            }

            return Content(statusMessage);
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
                PreviewDataDto previewDto = await mediator.Send(query, HttpContext.RequestAborted);
                PreviewDataModel previewModel = MapToPreviewDataModel(previewDto);
                var partialView = await RenderPartialViewAsync("_PreviewContent", previewModel);
                return Content(partialView, "text/html");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Preview: {ex.Message}");
                var errorModel = new PreviewDataModel
                {
                    StatusMessage = $"Error: {ex.Message}",
                    IsPreviewAvailable = false,
                };
                var partialView = await RenderPartialViewAsync("_PreviewContent", errorModel);
                return Content(partialView, "text/html");
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
            Application.Commands.Results.LoadDataResult result = await mediator.Send(command, HttpContext.RequestAborted);
            return new JsonResult(new
            {
                success = result.Success,
                importTableName = result.ImportTableName,
                statusMessage = result.StatusMessage,
            });
        }

        /// <summary>
        /// Generates a model prompt to translate from a selected import column to the current destination column.
        /// </summary>
        /// <param name="tmpdir">The temporary directory path.</param>
        /// <param name="mappingStep">The current mapping step (0-based).</param>
        /// <param name="selectedColumn">The selected import column name.</param>
        /// <returns>Partial view containing the generated prompt.</returns>
        public async Task<IActionResult> OnPostMappingTranslation(string tmpdir, int mappingStep, string selectedColumn)
        {
            try
            {
                var query = new GetMappingTranslationQuery(tmpdir, mappingStep, selectedColumn);
                MappingTranslationDto dto = await mediator.Send(query, HttpContext.RequestAborted);
                var model = new MappingTranslationModel { ModelPrompt = dto.ModelPrompt };
                var partialView = await RenderPartialViewAsync("_MappingTranslation", model);
                return Content(partialView, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<div class=\"text-danger\">error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</div>", "text/html");
            }
        }


        private async Task<string> RenderPartialViewAsync<T>(string partialName, T model)
        {
            Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine>();
            Microsoft.AspNetCore.Mvc.ViewEngines.ViewEngineResult viewResult = viewEngine.FindView(PageContext, partialName, false);

            if (!viewResult.Success)
            {
                throw new InvalidOperationException($"Partial view '{partialName}' not found");
            }

            var viewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<T>(ViewData, model);

            using var writer = new StringWriter();
            var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                PageContext,
                viewResult.View,
                viewData,
                TempData,
                writer: writer,
                htmlHelperOptions: new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return writer.ToString();
        }

        private static PreviewDataModel MapToPreviewDataModel(PreviewDataDto dto)
        {
            return new PreviewDataModel
            {
                StatusMessage = dto.StatusMessage,
                IsPreviewAvailable = dto.IsPreviewAvailable,
                ImportColumns = [.. dto.ImportColumns],
                ClaimsColumns = [.. dto.ClaimsColumns],
                PreviewRows = [.. dto.PreviewRows],
                ImportTableName = dto.ImportTableName,
                CurrentMappingStep = dto.CurrentMappingStep,
                ColumnMappings = new Dictionary<string, string>(dto.ColumnMappings, StringComparer.Ordinal),
                SelectedImportColumn = dto.SelectedImportColumn,
            };
        }

        /// <summary>
        /// Submits the current prompt to the AI service and returns the response markup.
        /// </summary>
        /// <param name="tmpdir">The temporary directory path (for consistency, not used).</param>
        /// <param name="promptText">The prompt text to send.</param>
        /// <returns>Partial view with the AI response content.</returns>
        public async Task<IActionResult> OnPostSubmitPromptToAI(string tmpdir, string promptText)
        {
            try
            {
                var cmd = new SubmitPromptToAICommand(tmpdir, promptText);
                AIResponseDto dto = await mediator.Send(cmd, HttpContext.RequestAborted);
                var partialView = await RenderPartialViewAsync("_AIResponse", dto);
                return Content(partialView, "text/html");
            }
            catch (Exception ex)
            {
                var err = new AIResponseDto { ResponseText = $"Error: {ex.Message}", IsSimulated = true };
                var partialView = await RenderPartialViewAsync("_AIResponse", err);
                return Content(partialView, "text/html");
            }
        }

        /// <summary>
        /// Saves a mapping from an import column to a claims (destination) column into the session config JSON.
        /// </summary>
        /// <param name="tmpdir">The temporary session directory.</param>
        /// <param name="outputColumn">Destination claims column name.</param>
        /// <param name="importColumn">Selected import/source column name.</param>
        /// <returns>A simple OK response.</returns>
        public async Task<IActionResult> OnPostSaveMapping(string tmpdir, string outputColumn, string importColumn)
        {
            var cmd = new SaveMappingCommand(tmpdir, outputColumn, importColumn);
            var ok = await mediator.Send(cmd, HttpContext.RequestAborted);
            return new JsonResult(new { success = ok });
        }

        /// <summary>
        /// Clears the saved mapping for the specified destination (claims) column from the session config.
        /// </summary>
        /// <param name="tmpdir">The temporary session directory.</param>
        /// <param name="outputColumn">Destination claims column name to clear.</param>
        /// <returns>JSON indicating success.</returns>
        public async Task<IActionResult> OnPostClearMapping(string tmpdir, string outputColumn)
        {
            var cmd = new ClearMappingCommand(tmpdir, outputColumn);
            var ok = await mediator.Send(cmd, HttpContext.RequestAborted);
            return new JsonResult(new { success = ok });
        }

        /// <summary>
        /// Downloads the current session configuration JSON file.
        /// </summary>
        /// <param name="tmpdir">The temporary session directory.</param>
        /// <returns>JSON file download.</returns>
        public async Task<IActionResult> OnGetDownloadConfig(string tmpdir)
        {
            if (string.IsNullOrWhiteSpace(tmpdir))
            {
                return NotFound("missing tmpdir");
            }

            var query = new GetConfigFileQuery(tmpdir);
            GetConfigFileResult res = await mediator.Send(query, HttpContext.RequestAborted);
            return File(res.Content, res.ContentType, res.FileName);
        }
    }
}
