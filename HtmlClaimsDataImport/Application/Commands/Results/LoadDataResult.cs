namespace HtmlClaimsDataImport.Application.Commands.Results
{
    /// <summary>
    /// Structured result for the LoadData command to avoid UI-coupled strings in the application layer.
    /// </summary>
    public class LoadDataResult
    {
        public bool Success { get; init; }
        public string StatusMessage { get; init; } = string.Empty;
        public string? ImportTableName { get; init; }

        public static LoadDataResult Ok(string importTableName, string? message = null)
            => new() { Success = true, ImportTableName = importTableName, StatusMessage = message ?? string.Empty };

        public static LoadDataResult Fail(string message)
            => new() { Success = false, ImportTableName = null, StatusMessage = message };
    }
}

