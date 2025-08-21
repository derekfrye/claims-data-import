namespace HtmlClaimsDataImport.Infrastructure.Services;

using HtmlClaimsDataImport.Application.Interfaces;

// ITempDirectoryService and TempDirectoryService moved to dedicated files to satisfy MA0048

/// <summary>
/// A hosted service that manages the cleanup of temporary directories created during the application's lifecycle.
/// </summary>
public class TempDirectoryCleanupService : IHostedService
{
    private static readonly List<ITempDirectoryService> ActiveServices = [];
    private static readonly System.Threading.Lock LockObject = new();
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
        using (LockObject.EnterScope())
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
        using (LockObject.EnterScope())
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
        using (LockObject.EnterScope())
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
