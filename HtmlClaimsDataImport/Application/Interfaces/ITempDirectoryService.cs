namespace HtmlClaimsDataImport.Application.Interfaces
{
    /// <summary>
    /// Defines methods for managing temporary directories, including creation, registration, and cleanup.
    /// Also provides policy to resolve a safe temp directory for uploads.
    /// </summary>
    public interface ITempDirectoryService
    {
        string GetSessionTempDirectory();
        string CreateTempDirectory();
        void RegisterTempDirectory(string path);
        void CleanupDirectories();

        /// <summary>
        /// Resolves which temp directory to use for an upload, validating an optional requested directory
        /// against the authorized base path and falling back to the session directory if invalid.
        /// Ensures the directory exists.
        /// </summary>
        /// <param name="requestedTmpDir">Optional requested tmpdir path.</param>
        /// <returns>Validated temp directory path to use.</returns>
        string ResolveUploadTempDirectory(string? requestedTmpDir);
    }

}
