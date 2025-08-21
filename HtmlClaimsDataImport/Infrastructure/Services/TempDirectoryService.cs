namespace HtmlClaimsDataImport.Infrastructure.Services;

/// <summary>
/// Provides functionality for managing temporary directories, including creation, registration, and cleanup.
/// </summary>
/// <param name="sessionId">The unique identifier for the session.</param>
/// <param name="basePath">The base path for temporary directories. If null, the system's temporary path is used.</param>
public class TempDirectoryService(string sessionId, string? basePath = null) : ITempDirectoryService, IDisposable
{
    private readonly List<string> tempDirectories = [];
    private readonly System.Threading.Lock lockObject = new();
    private readonly string basePath = basePath ?? Path.GetTempPath();
    private readonly string sessionId = sessionId;
    private string? sessionTempDirectory;

    public string GetSessionTempDirectory()
    {
        if (this.sessionTempDirectory == null)
        {
            using (this.lockObject.EnterScope())
            {
                this.sessionTempDirectory ??= this.CreateTempDirectory();
            }
        }
        return this.sessionTempDirectory;
    }

    public string CreateTempDirectory()
    {
        var tempDirName = $"session-{this.sessionId}-{Path.GetRandomFileName()}";
        var tempDir = Path.Combine(this.basePath, tempDirName);
        Directory.CreateDirectory(tempDir);
        this.RegisterTempDirectory(tempDir);
        return tempDir;
    }

    public void RegisterTempDirectory(string path)
    {
        using (this.lockObject.EnterScope())
        {
            this.tempDirectories.Add(path);
        }
    }

    public void CleanupDirectories()
    {
        using (this.lockObject.EnterScope())
        {
            foreach (var dir in this.tempDirectories)
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

            this.tempDirectories.Clear();
        }
    }

    public void Dispose()
    {
        // Do not cleanup directories on dispose - only cleanup on server termination
        // This allows files to persist across HTTP requests within the same session
    }
}

