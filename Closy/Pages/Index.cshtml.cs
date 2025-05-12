using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Closy.Data; // Added for ApplicationDbContext
using Closy.Models; // Ensure this is using the correct ClothingItem model
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Added for ToListAsync and CountAsync

namespace closy.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Added ApplicationDbContext

        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context) // Added ApplicationDbContext
        {
            _userManager = userManager;
            _context = context; // Added ApplicationDbContext
        }

        public ApplicationUser CurrentUser { get; set; }
        public int TotalItems { get; set; }
        public int SavedOutfits { get; set; }
        public int FavoriteItems { get; set; }
        public int OutfitsThisMonth { get; set; } // This might need a more complex query or be a placeholder

        public List<string> Categories { get; set; } = new List<string>();
        public List<Closy.Models.ClothingItem> RecentItems { get; set; } = new List<Closy.Models.ClothingItem>();

        public async Task OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser != null)
            {
                TotalItems = await _context.ClothingItems.CountAsync(ci => ci.UserId == CurrentUser.Id);
                FavoriteItems = await _context.ClothingItems.CountAsync(ci => ci.UserId == CurrentUser.Id && ci.IsFavorite);
                // SavedOutfits would query OutfitModels, assuming a relation to ApplicationUser
                SavedOutfits = await _context.OutfitModels.CountAsync(o => o.UserId == CurrentUser.Id); 

                RecentItems = await _context.ClothingItems
                                            .Where(ci => ci.UserId == CurrentUser.Id)
                                            .OrderByDescending(ci => ci.CreatedAt) // Assuming CreatedAt exists
                                            .Take(4) // Take a few recent items
                                            .ToListAsync();

                Categories = await _context.ClothingItems
                                        .Where(ci => ci.UserId == CurrentUser.Id && !string.IsNullOrEmpty(ci.Category))
                                        .Select(ci => ci.Category)
                                        .Distinct()
                                        .Take(5) // Take a few distinct categories
                                        .ToListAsync();
                
                // OutfitsThisMonth would require querying OutfitModels based on a creation date within the current month.
                // For now, it can remain a placeholder or be implemented if OutfitModel has a creation date.
                // Example: OutfitsThisMonth = await _context.OutfitModels.CountAsync(o => o.UserId == CurrentUser.Id && o.CreatedAt.Month == DateTime.UtcNow.Month && o.CreatedAt.Year == DateTime.UtcNow.Year);
            }
        }
    }
}