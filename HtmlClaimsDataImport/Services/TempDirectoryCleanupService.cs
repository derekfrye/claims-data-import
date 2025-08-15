namespace HtmlClaimsDataImport.Services;

public class TempDirectoryCleanupService : IHostedService
{
    private static readonly List<string> _tempDirectories = new();
    private static readonly object _lock = new();

    public static void RegisterTempDirectory(string path)
    {
        lock (_lock)
        {
            _tempDirectories.Add(path);
        }
    }

    public static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        RegisterTempDirectory(tempDir);
        return tempDir;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            foreach (var dir in _tempDirectories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, recursive: true);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw - we're shutting down
                    Console.WriteLine($"Failed to delete temp directory {dir}: {ex.Message}");
                }
            }
            _tempDirectories.Clear();
        }
        
        return Task.CompletedTask;
    }
}