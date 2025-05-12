using System;
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

namespace closy.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public ApplicationUser CurrentAdmin { get; set; }
        public int TotalUsers { get; set; }
        public int TotalItems { get; set; }
        public double TotalStorage { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();

        public async Task OnGetAsync()
        {
            // Get current admin user
            CurrentAdmin = await _userManager.GetUserAsync(User);

            // Get real users count
            TotalUsers = await _context.Users.CountAsync();

            // Get real items count from ClothingItem
            TotalItems = await _context.ClothingItems.CountAsync();

            // Calculate storage used from clothing images
            string uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            TotalStorage = GetDirectorySize(uploadsPath) / (1024.0 * 1024.0 * 1024.0); // Convert to GB

            // Get 5 most recent users with their actual registration date
            RecentUsers = await _context.Users
                .OrderByDescending(u => u.DataRegistrazione)
                .Take(5)
                .ToListAsync();
        }

        private double GetDirectorySize(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return 0;

            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
    }
}