namespace LibClaimsDataImport.Importer;

public class MoneyFormats
{
    public bool AllowParenthesesForNegative { get; set; } = true;
    public bool StripCurrencySymbols { get; set; } = true;
    public bool StripThousandsSeparators { get; set; } = true;
    public string DefaultCurrency { get; set; } = "USD";
}

