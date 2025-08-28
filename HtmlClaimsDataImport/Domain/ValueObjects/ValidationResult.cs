namespace HtmlClaimsDataImport.Domain.ValueObjects
{
    public record ValidationResult(bool IsValid, string ErrorMessage)
    {
        public static ValidationResult Success() => new(true, string.Empty);
        public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
    }
}
