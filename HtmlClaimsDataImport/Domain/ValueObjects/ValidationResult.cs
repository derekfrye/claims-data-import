namespace HtmlClaimsDataImport.Domain.ValueObjects
{
    public record ValidationResult(bool IsValid, string ErrorMessage)
    {
        public static ValidationResult Success()
        {
            return new ValidationResult(IsValid: true, ErrorMessage: string.Empty);
        }

        public static ValidationResult Failure(string errorMessage)
        {
            return new ValidationResult(IsValid: false, ErrorMessage: errorMessage);
        }
    }
}
