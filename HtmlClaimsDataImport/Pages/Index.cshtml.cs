
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmlClaimsDataImport.Pages
{
    /// <summary>
    /// Represents the model for the Index page.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    public class IndexModel(ILogger<IndexModel> logger) : PageModel
    {
        private readonly ILogger<IndexModel> logger = logger;

        /// <summary>
        /// Handles GET requests for the Index page.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
