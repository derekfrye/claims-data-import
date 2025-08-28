
using HtmlClaimsDataImport.Application.Interfaces;

namespace HtmlClaimsDataImport.Infrastructure.Services
{
    /// <summary>
    /// Provides functionality for managing temporary directories, including creation, registration, and cleanup.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session.</param>
    /// <param name="basePath">The base path for temporary directories. If null, the system's temporary path is used.</param>
    public class TempDirectoryService(string sessionId, string? basePath = null) : ITempDirectoryService, IDisposable
    {
        private readonly List<string> tempDirectories = [];
        private readonly Lock lockObject = new();
        private readonly string basePath = basePath ?? Path.GetTempPath();
        private readonly string sessionId = sessionId;
        private string? sessionTempDirectory;

        public string GetSessionTempDirectory()
        {
            if (sessionTempDirectory == null)
            {
                using (lockObject.EnterScope())
                {
                    sessionTempDirectory ??= CreateTempDirectory();
                }
            }
            return sessionTempDirectory;
        }

        public string CreateTempDirectory()
        {
            var tempDirName = $"session-{sessionId}-{Path.GetRandomFileName()}";
            var tempDir = Path.Combine(basePath, tempDirName);
            _ = Directory.CreateDirectory(tempDir);
            RegisterTempDirectory(tempDir);
            return tempDir;
        }

        public void RegisterTempDirectory(string path)
        {
            using (lockObject.EnterScope())
            {
                tempDirectories.Add(path);
            }
        }

        public void CleanupDirectories()
        {
            using (lockObject.EnterScope())
            {
                foreach (var dir in tempDirectories)
                {
                    try
                    {
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, recursive: true);
                            Console.WriteLine($"Cleaned up temp directory: {dir}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete temp directory {dir}: {ex.Message}");
                    }
                }

                tempDirectories.Clear();
            }
        }

        public void Dispose()
        {
            // Do not cleanup directories on dispose - only cleanup on server termination
            // This allows files to persist across HTTP requests within the same session
        }

        public string ResolveUploadTempDirectory(string? requestedTmpDir)
        {
            if (string.IsNullOrEmpty(requestedTmpDir))
            {
                return GetSessionTempDirectory();
            }

            var normalizedRequested = Path.GetFullPath(requestedTmpDir);
            var normalizedBase = Path.GetFullPath(basePath);

            if (!normalizedRequested.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"⚠️  SECURITY WARNING: tmpdir parameter '{requestedTmpDir}' is outside authorized base path '{basePath}'. Reverting to session-based logic.");
                return GetSessionTempDirectory();
            }

            if (!Directory.Exists(requestedTmpDir))
            {
                _ = Directory.CreateDirectory(requestedTmpDir);
            }

            RegisterTempDirectory(requestedTmpDir);
            return requestedTmpDir;
        }
    }
}
