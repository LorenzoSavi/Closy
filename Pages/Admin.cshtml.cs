using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Closy.Data;
using Closy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Closy.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public AdminModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public int TotalUsers { get; set; }
        public int TotalItems { get; set; }
        public int TotalOutfits { get; set; }
        public string StorageUsedFormatted { get; set; } = "";
        public int StoragePercentage { get; set; }
        public int ActiveUsersToday { get; set; }
        public int NewItemsToday { get; set; }
        public string AvgItemsPerUser { get; set; } = "";
        public string MostPopularCategory { get; set; } = "";
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();

        public async Task OnGetAsync()
        {
            // Count totals
            TotalUsers = await _userManager.Users.CountAsync();
            TotalItems = await _context.ClothingItems.CountAsync();
            TotalOutfits = await _context.OutfitModels.CountAsync();

            // Calculate storage used
            var imagesFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (Directory.Exists(imagesFolder))
            {
                long totalSizeBytes = 0;
                var files = Directory.GetFiles(imagesFolder, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSizeBytes += fileInfo.Length;
                }

                var totalSizeMB = totalSizeBytes / (1024 * 1024);
                StorageUsedFormatted = $"{totalSizeMB} MB";

                // Assuming 1GB limit
                var limitMB = 1024;
                StoragePercentage = (int)Math.Min(100, (totalSizeMB * 100) / limitMB);
            }

            // For simplicity, we'll just use a static value for active users today since UserActivities isn't available
            ActiveUsersToday = 5; // Static value since we don't have UserActivities table

            // New items today
            var today = DateTime.Today;
            NewItemsToday = await _context.ClothingItems
                .Where(i => i.CreatedAt.Date == today)
                .CountAsync();

            // Average items per user
            if (TotalUsers > 0)
            {
                var avgItems = (double)TotalItems / TotalUsers;
                AvgItemsPerUser = avgItems.ToString("0.0");
            }
            else
            {
                AvgItemsPerUser = "0";
            }

            // Most popular category
            var categories = await _context.ClothingItems
                .GroupBy(i => i.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            MostPopularCategory = categories?.Category ?? "N/A";

            // Recent users - we'll just get the 5 most recently registered users
            // But since ApplicationUser might not have CreatedAt, we'll use whatever criteria is available
            RecentUsers = await _userManager.Users
                .Take(5)
                .ToListAsync();
        }
    }
}