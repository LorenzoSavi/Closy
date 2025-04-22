using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Closy.Data;
using Closy.Models;

namespace Closy.Pages.UserProfiles.Interno
{
    public class HomeModel : BaseAuthenticatedPage
    {
        private readonly AppDbContext _context;

        public HomeModel(AppDbContext context)
        {
            _context = context;
        }

        public UserProfile UserProfile { get; set; }
        public IEnumerable<ClothingItem> ClothingItems { get; set; }
        public IEnumerable<string> Categories { get; set; }
        public int TotalItems { get; set; }
        public string CurrentCategory { get; set; }

        public async Task<IActionResult> OnGetAsync(string category = null)
        {
            var authCheck = CheckAuth();
            if (authCheck != null)
                return authCheck;

            UserProfile = await _context.UserProfiles.FindAsync(UserId.Value);
            if (UserProfile == null)
                return RedirectToPage("/UserProfiles/login");

            var query = _context.ClothingItems.Where(c => c.UserProfileId == UserId.Value);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(c => c.Category == category);
                CurrentCategory = category;
            }

            ClothingItems = await query.ToListAsync();
            Categories = await _context.ClothingItems
                .Where(c => c.UserProfileId == UserId.Value)
                .Select(c => c.Category)
                .Distinct()
                .ToListAsync();

            TotalItems = await _context.ClothingItems
                .Where(c => c.UserProfileId == UserId.Value)
                .CountAsync();

            return Page();
        }
    }
}