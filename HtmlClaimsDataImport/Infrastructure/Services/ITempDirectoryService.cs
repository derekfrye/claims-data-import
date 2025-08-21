namespace HtmlClaimsDataImport.Infrastructure.Services;

/// <summary>
/// Defines methods for managing temporary directories, including creation, registration, and cleanup.
/// </summary>
public interface ITempDirectoryService
{
    string GetSessionTempDirectory();
    string CreateTempDirectory();
    void RegisterTempDirectory(string path);
    void CleanupDirectories();
}

