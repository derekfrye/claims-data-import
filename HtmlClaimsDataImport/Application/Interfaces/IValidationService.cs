namespace HtmlClaimsDataImport.Application.Interfaces
{
    using HtmlClaimsDataImport.Domain.ValueObjects;

    public interface IValidationService
    {
        ValidationResult ValidateFile(string filePath);
        ValidationResult ValidateJsonFile(string jsonPath);
        ValidationResult ValidateSqliteDatabase(string databasePath);
    }
}