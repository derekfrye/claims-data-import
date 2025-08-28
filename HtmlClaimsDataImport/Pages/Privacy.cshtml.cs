
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmlClaimsDataImport.Pages
{
    public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
    {
        private readonly ILogger<PrivacyModel> logger = logger;

        public void OnGet()
        {
        }
    }
}