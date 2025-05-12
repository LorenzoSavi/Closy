using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Closy.Data;
using Closy.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public ClothingItem? ClothingItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ClothingItem = await _context.ClothingItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (ClothingItem == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            ClothingItem = await _context.ClothingItems
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == user.Id);

            if (ClothingItem != null)
            {
                _context.ClothingItems.Remove(ClothingItem);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Capo eliminato con successo.";
            }
            else
            {
                TempData["ErrorMessage"] = "Impossibile trovare il capo da eliminare o non autorizzato.";
                return NotFound();
            }

            return RedirectToPage("./AllItems");
        }
    }
}
