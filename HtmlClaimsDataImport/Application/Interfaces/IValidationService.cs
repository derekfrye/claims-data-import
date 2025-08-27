namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Domain.ValueObjects;

    public interface IValidationService
    {
        Task<ValidationResult> ValidateFileAsync(string filePath);
        Task<ValidationResult> ValidateJsonFileAsync(string jsonPath);
        Task<ValidationResult> ValidateSqliteDatabaseAsync(string databasePath);
    }
}
