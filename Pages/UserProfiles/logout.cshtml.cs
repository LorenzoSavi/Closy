using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Closy.Pages.UserProfiles
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Rimuovi tutti i dati dalla sessione
            HttpContext.Session.Clear();

            // Imposta un messaggio di conferma
            TempData["Message"] = "Logout effettuato con successo!";

            // Reindirizza alla pagina iniziale
            return RedirectToPage("/Index");
        }
    }
}