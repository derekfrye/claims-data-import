namespace HtmlClaimsDataImport.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;

public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
{
    private readonly ILogger<PrivacyModel> logger = logger;

    public void OnGet()
    {
    }
}