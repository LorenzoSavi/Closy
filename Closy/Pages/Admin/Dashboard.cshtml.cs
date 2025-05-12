using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Closy.Data;
using Closy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace closy.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public ApplicationUser? CurrentAdmin { get; set; }
        public int TotalUsers { get; set; }
        public int TotalItems { get; set; }
        public double TotalStorage { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();

        public IList<UserWithRoles> UsersList { get; set; } = new List<UserWithRoles>();

        public class UserWithRoles
        {
            public ApplicationUser User { get; set; } = null!;
            public List<string> Roles { get; set; } = new List<string>();
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

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

            // Load users for the user management table
            var users = await _context.Users.OrderByDescending(u => u.DataRegistrazione).ToListAsync();
            UsersList = new List<UserWithRoles>();

            foreach (var user in users)
            {
                var userWithRoles = new UserWithRoles
                {
                    User = user,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                };
                UsersList.Add(userWithRoles);
            }
        }

        private double GetDirectorySize(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return 0;

            try
            {
                DirectoryInfo di = new DirectoryInfo(folderPath);
                return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating directory size for {folderPath}: {ex.Message}");
                return 0;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                StatusMessage = "Errore: ID utente non fornito.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                StatusMessage = "Errore: Utente non trovato.";
                return RedirectToPage();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (user.Id == currentUser?.Id)
            {
                StatusMessage = "Errore: Non puoi eliminare te stesso.";
                return RedirectToPage();
            }

            if (user.Email != null && user.Email.Equals("admin@closy.com", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Errore: Non puoi eliminare l'amministratore principale.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Utente '{user.UserName}' eliminato con successo.";
            }
            else
            {
                StatusMessage = $"Errore durante l'eliminazione dell'utente '{user.UserName}'.";
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    StatusMessage += $" {error.Description}";
                }
            }
            return RedirectToPage();
        }
    }
}