
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmlClaimsDataImport.Pages
{
    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
            _ = HttpContext; // touch instance to avoid static suggestion
        }
    }
}
