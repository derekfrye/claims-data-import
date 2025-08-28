using HtmlClaimsDataImport.Domain.ValueObjects;

namespace HtmlClaimsDataImport.Application.Interfaces
{
    public interface IValidationService
    {
        Task<ValidationResult> ValidateFileAsync(string filePath);
        Task<ValidationResult> ValidateJsonFileAsync(string jsonPath);
        Task<ValidationResult> ValidateSqliteDatabaseAsync(string databasePath);
    }
}
