namespace HtmlClaimsDataImport.Infrastructure.Services;

/// <summary>
/// Defines methods for managing temporary directories, including creation, registration, and cleanup.
/// </summary>
public interface ITempDirectoryService
{
    /// <summary>
    /// Gets the temporary directory for the current session.
    /// </summary>
    /// <returns>The path to the session's temporary directory.</returns>
    string GetSessionTempDirectory();

    /// <summary>
    /// Creates a new temporary directory and returns its path.
    /// </summary>
    /// <returns>The path to the newly created temporary directory.</returns>
    string CreateTempDirectory();

    /// <summary>
    /// Registers a temporary directory for cleanup.
    /// </summary>
    /// <param name="path">The path to the temporary directory to register.</param>
    void RegisterTempDirectory(string path);

    /// <summary>
    /// Cleans up all registered temporary directories.
    /// </summary>
    void CleanupDirectories();
}

/// <summary>
/// Provides functionality for managing temporary directories, including creation, registration, and cleanup.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TempDirectoryService"/> class.
/// </remarks>
/// <param name="sessionId">The unique identifier for the session.</param>
/// <param name="basePath">The base path for temporary directories. If null, the system's temporary path is used.</param>
public class TempDirectoryService(string sessionId, string? basePath = null) : ITempDirectoryService, IDisposable
{
    private readonly List<string> tempDirectories = [];

    private readonly object lockObject = new ();

    private readonly string basePath = basePath ?? Path.GetTempPath();
    private readonly string sessionId = sessionId;
    private string? sessionTempDirectory;

    /// <summary>
    /// Gets the temporary directory for the current session. Creates it if it does not exist.
    /// </summary>
    /// <returns>The path to the session's temporary directory.</returns>
    public string GetSessionTempDirectory()
    {
        if (this.sessionTempDirectory == null)
        {
            lock (this.lockObject)
            {
                this.sessionTempDirectory ??= this.CreateTempDirectory();
            }
        }

        return this.sessionTempDirectory;
    }

    /// <summary>
    /// Creates a new temporary directory, registers it for cleanup, and returns its path.
    /// </summary>
    /// <returns>The path to the newly created temporary directory.</returns>
    public string CreateTempDirectory()
    {
        var tempDirName = $"session-{this.sessionId}-{Path.GetRandomFileName()}";
        var tempDir = Path.Combine(this.basePath, tempDirName);
        Directory.CreateDirectory(tempDir);
        this.RegisterTempDirectory(tempDir);
        return tempDir;
    }

    /// <summary>
    /// Registers a temporary directory for cleanup.
    /// </summary>
    /// <param name="path">The path to the temporary directory to register.</param>
    public void RegisterTempDirectory(string path)
    {
        lock (this.lockObject)
        {
            this.tempDirectories.Add(path);
        }
    }

    /// <summary>
    /// Cleans up all registered temporary directories by deleting them and clearing the list.
    /// </summary>
    public void CleanupDirectories()
    {
        lock (this.lockObject)
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

    /// <summary>
    /// Disposes the service. Cleanup only occurs on server termination, not on request disposal.
    /// </summary>
    public void Dispose()
    {
        // Do not cleanup directories on dispose - only cleanup on server termination
        // This allows files to persist across HTTP requests within the same session
    }
}

/// <summary>
/// A hosted service that manages the cleanup of temporary directories created during the application's lifecycle.
/// </summary>
public class TempDirectoryCleanupService : IHostedService
{
    private static readonly List<ITempDirectoryService> ActiveServices = [];
    private static readonly object LockObject = new ();
    private static string? overrideTempBasePath;

    /// <summary>
    /// Sets the base path for temporary directories. If set, this path will override the system's default temporary path.
    /// </summary>
    /// <param name="basePath">The base path to use for temporary directories, or null to use the default system path.</param>
    public static void SetTempBasePath(string? basePath)
    {
        overrideTempBasePath = basePath;
    }

    /// <summary>
    /// Registers a temporary directory service for cleanup management.
    /// </summary>
    /// <param name="service">The temporary directory service to register.</param>
    public static void RegisterService(ITempDirectoryService service)
    {
        lock (LockObject)
        {
            ActiveServices.Add(service);
        }
    }

    /// <summary>
    /// Unregisters a temporary directory service, removing it from cleanup management.
    /// </summary>
    /// <param name="service">The temporary directory service to unregister.</param>
    public static void UnregisterService(ITempDirectoryService service)
    {
        lock (LockObject)
        {
            ActiveServices.Remove(service);
        }
    }

    /// <summary>
    /// Gets the base path for temporary directories. Returns the overridden path if set; otherwise, the system's temporary path.
    /// </summary>
    /// <returns>The base path for temporary directories.</returns>
    public static string GetTempBasePath()
    {
        return overrideTempBasePath ?? Path.GetTempPath();
    }

    /// <summary>
    /// Cleans up all temporary directories registered by active services.
    /// </summary>
    public static void CleanupAllDirectories()
    {
        lock (LockObject)
        {
            var servicesToCleanup = new List<ITempDirectoryService>(ActiveServices);
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

            ActiveServices.Clear();
        }
    }

    /// <summary>
    /// Starts the service. This method is called at the start of the application.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the service and cleans up all temporary directories.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("StopAsync: Cleaning up temp directories...");
        CleanupAllDirectories();
        return Task.CompletedTask;
    }
}