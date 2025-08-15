namespace HtmlClaimsDataImport.Services;

public class TempDirectoryCleanupService : IHostedService
{
    private static readonly List<string> _tempDirectories = new();
    private static readonly object _lock = new();
    private static string? _sessionTempDirectory;
    private static string? _overrideTempBasePath;

    public static void SetTempBasePath(string? basePath)
    {
        _overrideTempBasePath = basePath;
    }

    public static void RegisterTempDirectory(string path)
    {
        lock (_lock)
        {
            _tempDirectories.Add(path);
        }
    }

    public static string CreateTempDirectory()
    {
        var basePath = _overrideTempBasePath ?? Path.GetTempPath();
        var tempDir = Path.Combine(basePath, Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        RegisterTempDirectory(tempDir);
        return tempDir;
    }

    public static string GetSessionTempDirectory()
    {
        if (_sessionTempDirectory == null)
        {
            lock (_lock)
            {
                _sessionTempDirectory ??= CreateTempDirectory();
            }
        }
        return _sessionTempDirectory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public static void CleanupAllDirectories()
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
                        Console.WriteLine($"Cleaned up temp directory: {dir}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete temp directory {dir}: {ex.Message}");
                }
            }
            _tempDirectories.Clear();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StopAsync: Cleaning up temp directories...");
        CleanupAllDirectories();
        return Task.CompletedTask;
    }
}