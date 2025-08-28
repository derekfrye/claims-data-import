
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmlClaimsDataImport.Pages
{
    /// <summary>
    /// Represents the model for the Index page.
    /// </summary>
    public class IndexModel : PageModel
    {

        /// <summary>
        /// Handles GET requests for the Index page.
        /// </summary>
        public void OnGet()
        {
            _ = HttpContext; // touch instance to avoid static suggestion
        }
    }
}
