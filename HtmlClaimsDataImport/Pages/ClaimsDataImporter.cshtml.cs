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
        /// Formats a file size in bytes to a human-readable string.
        /// </summary>
        /// <param name="bytes">The file size in bytes.</param>
        /// <returns>A formatted string representing the file size.</returns>
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

        /// <summary>
        /// Validates if a JSON file is valid and readable.
        /// </summary>
        /// <param name="filePath">The path to the JSON file.</param>
        /// <returns>True if the JSON file is valid, false otherwise.</returns>
        private static bool IsValidJsonFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return false;
                }

                var jsonContent = System.IO.File.ReadAllText(filePath);
                JsonSerializer.Deserialize<object>(jsonContent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a text file for readability and existence.
        /// </summary>
        /// <param name="filePath">The path to the text file.</param>
        /// <returns>A validation result containing success status and error message if applicable.</returns>
        private static (bool IsValid, string ErrorMessage) ValidateTextFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return (false, "File does not exist");
                }

                using var reader = new StreamReader(filePath);
                reader.ReadLine(); // Try to read first line
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"File validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates a SQLite database file for readability and structure.
        /// </summary>
        /// <param name="filePath">The path to the SQLite database file.</param>
        /// <returns>A validation result containing success status and error message if applicable.</returns>
        private static (bool IsValid, string ErrorMessage) ValidateSqliteDatabase(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    return (false, "Database file does not exist");
                }

                using var connection = new SqliteConnection($"Data Source={filePath}");
                connection.Open();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Database validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a random table name for database operations.
        /// </summary>
        /// <param name="databasePath">The path to the database (used for validation).</param>
        /// <returns>A randomly generated table name.</returns>
        private static string GenerateRandomTableName(string databasePath)
        {
            var random = new Random();
            var randomId = random.Next(10000, 99999);
            return $"claims_import_{randomId}";
        }

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
                string actualDatabasePath = await this.ResolveDatabasePathAsync(databasePath, tmpdir);
                string actualFileName = Path.Combine(tmpdir, fileName);

                // Validation step 1: Check if JSON is valid
                if (!IsValidJsonFile(actualJsonPath))
                {
                    return this.Content("json invalid");
                }

                // Validation step 2: Check if file exists and is readable
                var fileValidation = ValidateTextFile(actualFileName);
                if (!fileValidation.IsValid)
                {
                    return this.Content(fileValidation.ErrorMessage);
                }

                // Validation step 3: Check if database exists and is readable as SQLite
                var dbValidation = ValidateSqliteDatabase(actualDatabasePath);
                if (!dbValidation.IsValid)
                {
                    return this.Content(dbValidation.ErrorMessage);
                }

                // All validations passed - proceed with import
                var result = await this.ProcessFileImport(actualFileName, actualJsonPath, actualDatabasePath);
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

        private async Task<string> ResolveDatabasePathAsync(string path, string tmpdir)
        {
            if (path == "default")
            {
                // Copy default database to temp directory
                string defaultDbPath = Path.Combine(Directory.GetCurrentDirectory(), this.DefaultDatabase);
                string tempDbFileName = $"working_db_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                string tempDbPath = Path.Combine(tmpdir, tempDbFileName);

                // Copy the default database to temp directory
                await this.CopyFileAsync(defaultDbPath, tempDbPath);
                return tempDbPath;
            }
            else
            {
                // For uploaded databases, copy to temp directory with a working copy name
                string sourceDbPath = Path.IsPathRooted(path) ? path : Path.Combine(tmpdir, path);
                string tempDbFileName = $"working_db_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                string tempDbPath = Path.Combine(tmpdir, tempDbFileName);

                // Copy the uploaded database to a working copy
                await this.CopyFileAsync(sourceDbPath, tempDbPath);
                return tempDbPath;
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
            await sourceStream.CopyToAsync(destinationStream);
        }

        private async Task<string> ProcessFileImport(string fileName, string jsonPath, string databasePath)
        {
            try
            {
                // Step 4a: Setup stream reader
                using var streamReader = new StreamReader(fileName);

                // Step 4b: Setup FileSpec and scan
                var scanCsvReader = CsvDataReader.Create(streamReader, new CsvDataReaderOptions { HasHeaders = true });
                var fileSpec = new FileSpec(scanCsvReader);
                fileSpec.Scan();

                // Step 4c: Reset stream
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                streamReader.DiscardBufferedData();

                // Step 4d: Load ImportConfig
                ImportConfig? config = null;
                if (jsonPath != "default")
                {
                    config = ImportConfig.LoadFromFile(jsonPath);
                }

                // Step 4e: Create File instance
                var file = LibClaimsDataImport.Importer.File.New(streamReader, fileSpec, config);

                // Step 4f: Generate random table name
                string tableName = GenerateRandomTableName(databasePath);

                // Step 4g: Write to database
                await file.WriteToDb(databasePath, tableName);

                return $"file imported to table '{tableName}' in temp database";
            }
            catch (Exception ex)
            {
                return $"Import failed: {ex.Message}";
            }
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