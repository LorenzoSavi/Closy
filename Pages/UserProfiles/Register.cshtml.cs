using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Closy.Data;
using Closy.Models;

namespace Closy.Pages.UserProfiles
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Le password non corrispondono");
                return Page();
            }

            if (!ModelState.IsValid)
                return Page();

            var user = new UserProfile
            {
                Username = Username,
                Email = Email,
                Password = Password // In un'app reale, qui dovresti hashare la password!
            };

            _context.UserProfiles.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToPage("/UserProfiles/Interno/Home");
        }
    }
}