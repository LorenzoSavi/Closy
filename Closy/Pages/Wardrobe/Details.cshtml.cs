using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Closy.Data;
using Closy.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Required for UserManager
using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Closy.Services; // For IGeminiService
using System.IO; // For Path.Combine
using Microsoft.AspNetCore.Hosting; // For IWebHostEnvironment

namespace Closy.Pages.Wardrobe // Ensure this namespace is correct
{
    [Authorize] // Ensure only authorized users can access
    public class DetailsModel : PageModel // Ensure class name is correct
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeminiService _geminiService;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IGeminiService geminiService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _geminiService = geminiService;
            _webHostEnvironment = webHostEnvironment;
        }

        public ClothingItem? ClothingItem { get; set; } // Made nullable
        public string? AnalysisResult { get; set; } // Made nullable
        public bool IsImageAnalyzed { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? id, bool analyze = false)
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

            if (analyze && !string.IsNullOrEmpty(ClothingItem.ImageUrl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, ClothingItem.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    // AnalysisResult = await _geminiService.AnalyzeImageAsync(imagePath, "Describe this clothing item.");
                    AnalysisResult = "L'analisi dell'immagine non è ancora implementata o la funzione AnalyzeImageAsync non è disponibile.";
                }
                else
                {
                    AnalysisResult = "Immagine non trovata per l'analisi.";
                }
                IsImageAnalyzed = true; // Set to true to show the analysis section
            }

            return Page();
        }
    }
}
