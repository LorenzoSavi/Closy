using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Closy.Data;
using Closy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class AllItemsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AllItemsModel> _logger;

        public AllItemsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AllItemsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            ClothingItems = new List<ClothingItem>();
        }

        public IList<ClothingItem> ClothingItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("AllItems.OnGetAsync called.");
            if (!_signInManager.IsSignedIn(User))
            {
                _logger.LogWarning("User not signed in. Redirecting to Index.");
                return RedirectToPage("/Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found. Redirecting to Index.");
                return RedirectToPage("/Index");
            }
            _logger.LogInformation("User {UserId} retrieved.", user.Id);

            var query = _context.ClothingItems
                                 .Where(c => c.UserId == user.Id);

            ClothingItems = await query.ToListAsync();
            _logger.LogInformation("Retrieved {Count} items for user {UserId}.", ClothingItems.Count, user.Id);

            return Page();
        }

        public async Task<JsonResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Utente non autorizzato." }) { StatusCode = 401 };
            }

            var clothingItem = await _context.ClothingItems
                .FirstOrDefaultAsync(ci => ci.Id == id && ci.UserId == user.Id);

            if (clothingItem == null)
            {
                return new JsonResult(new { success = false, message = "Capo non trovato o non autorizzato." }) { StatusCode = 404 };
            }

            _context.ClothingItems.Remove(clothingItem);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Capo eliminato con successo." });
        }
    }
}