using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Closy.Data;
using Closy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class FavoritesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public FavoritesModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<ClothingItem> ClothingItems { get; set; }
        public int FavoriteItems { get; set; }
        public int TotalItems { get; set; }
        public List<string> Categories { get; set; }
        public Dictionary<string, double> CategoryDistribution { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return NotFound();
            }

            // Ottieni tutti i capi preferiti dell'utente
            ClothingItems = await _dbContext.ClothingItems
                .Where(i => i.UserId == CurrentUser.Id && i.IsFavorite)
                .ToListAsync();

            // Calcola le statistiche
            FavoriteItems = ClothingItems.Count;
            TotalItems = await _dbContext.ClothingItems
                .CountAsync(i => i.UserId == CurrentUser.Id);

            // Ottieni le categorie uniche
            Categories = await _dbContext.ClothingItems
                .Where(i => i.UserId == CurrentUser.Id)
                .Select(i => i.Category)
                .Distinct()
                .ToListAsync();

            // Calcola la distribuzione per categoria
            CategoryDistribution = ClothingItems
                .GroupBy(i => i.Category)
                .ToDictionary(
                    g => g.Key,
                    g => FavoriteItems > 0
                        ? Math.Round((double)g.Count() / FavoriteItems * 100, 1)
                        : 0
                );

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(int itemId)
        {
            var item = await _dbContext.ClothingItems
                .FirstOrDefaultAsync(i => i.Id == itemId && i.UserId == _userManager.GetUserId(User));

            if (item != null)
            {
                item.IsFavorite = false;
                await _dbContext.SaveChangesAsync();
                return new JsonResult(new { success = true });
            }

            return new JsonResult(new { success = false });
        }
    }
}