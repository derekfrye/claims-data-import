namespace HtmlClaimsDataImport.Services;

public interface ITempDirectoryService
{
    string GetSessionTempDirectory();
    string CreateTempDirectory();
    void RegisterTempDirectory(string path);
    void CleanupDirectories();
}

public class TempDirectoryService : ITempDirectoryService, IDisposable
{
    private readonly List<string> _tempDirectories = new();
    private readonly object _lock = new();
    private readonly string _basePath;
    private readonly string _sessionId;
    private string? _sessionTempDirectory;

    public TempDirectoryService(string sessionId, string? basePath = null)
    {
        _sessionId = sessionId;
        _basePath = basePath ?? Path.GetTempPath();
    }

    public string GetSessionTempDirectory()
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

    public string CreateTempDirectory()
    {
        var tempDirName = $"session-{_sessionId}-{Path.GetRandomFileName()}";
        var tempDir = Path.Combine(_basePath, tempDirName);
        Directory.CreateDirectory(tempDir);
        RegisterTempDirectory(tempDir);
        return tempDir;
    }

    public void RegisterTempDirectory(string path)
    {
        lock (_lock)
        {
            _tempDirectories.Add(path);
        }
    }

    public void CleanupDirectories()
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

    public void Dispose()
    {
        CleanupDirectories();
    }
}

public class TempDirectoryCleanupService : IHostedService
{
    private static readonly List<ITempDirectoryService> _activeServices = new();
    private static readonly object _lock = new();
    private static string? _overrideTempBasePath;

    public static void SetTempBasePath(string? basePath)
    {
        _overrideTempBasePath = basePath;
    }

    public static void RegisterService(ITempDirectoryService service)
    {
        lock (_lock)
        {
            _activeServices.Add(service);
        }
    }

    public static void UnregisterService(ITempDirectoryService service)
    {
        lock (_lock)
        {
            _activeServices.Remove(service);
        }
    }

    public static string GetTempBasePath()
    {
        return _overrideTempBasePath ?? Path.GetTempPath();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public static void CleanupAllDirectories()
    {
        lock (_lock)
        {
            var servicesToCleanup = new List<ITempDirectoryService>(_activeServices);
            foreach (var service in servicesToCleanup)
            {
                try
                {
                    service.CleanupDirectories();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to cleanup service directories: {ex.Message}");
                }
            }
            _activeServices.Clear();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StopAsync: Cleaning up temp directories...");
        CleanupAllDirectories();
        return Task.CompletedTask;
    }
}