using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Closy.Pages.UserProfiles.Interno
{
    public class BaseAuthenticatedPage : PageModel
    {
        protected int? UserId => HttpContext.Session.GetInt32("UserId");

        public IActionResult CheckAuth()
        {
            if (!UserId.HasValue)
            {
                return RedirectToPage("/UserProfiles/login");
            }
            return null;
        }
    }
}