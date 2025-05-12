using Closy.Data;
using Closy.Models;
using Closy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class GenerateOutfitModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IClothingItemService _clothingItemService;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<GenerateOutfitModel> _logger;

        public GenerateOutfitModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IClothingItemService clothingItemService,
            IGeminiService geminiService,
            ILogger<GenerateOutfitModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _clothingItemService = clothingItemService;
            _geminiService = geminiService;
            _logger = logger;
            AvailableColors = GetDefaultColors(); // Initialize with default colors
            AvailableSeasons = new List<string> { "Primavera", "Estate", "Autunno", "Inverno" };
        }

        public ApplicationUser CurrentUser { get; set; }
        public List<ClothingItem> AllClothingItems { get; set; } = new List<ClothingItem>();

        [BindProperty]
        public List<int> SelectedItemIds { get; set; } = new List<int>();

        [BindProperty]
        public List<string> SelectedColors { get; set; } = new List<string>();

        [BindProperty]
        public List<string> SelectedSeasons { get; set; } = new List<string>();

        [BindProperty]
        public string Occasion { get; set; }

        public List<(string Hex, string Name)> AvailableColors { get; set; } // Changed type
        public List<string> AvailableSeasons { get; set; }

        public List<Closy.Services.GeneratedOutfit> GeneratedOutfits { get; set; }
        public string ErrorMessage { get; set; }

        private List<(string Hex, string Name)> GetDefaultColors() // Changed return type and content
        {
            return new List<(string Hex, string Name)>
            {
                ("#000000", "Nero"), ("#FFFFFF", "Bianco"), ("#0047AB", "Blu"),
                ("#DC3545", "Rosso"), ("#28A745", "Verde"), ("#FFC107", "Giallo"),
                ("#6A5ACD", "Viola"), ("#FF69B4", "Rosa"), ("#A52A2A", "Marrone"),
                ("#808080", "Grigio"), ("#F5F5DC", "Beige"), ("#FFA500", "Arancione")
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return NotFound("Utente non trovato.");
            }

            AllClothingItems = await _clothingItemService.GetUserClothingItemsAsync(CurrentUser.Id);
            // Optionally, populate AvailableColors from user's items if desired, in addition to defaults
            // var userColors = AllClothingItems.Select(i => i.ColorName).Distinct().ToList(); // Assuming ColorName property exists
            // AvailableColors.AddRange(userColors);
            // AvailableColors = AvailableColors.Distinct().OrderBy(c => c).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return NotFound("Utente non trovato.");
            }

            AllClothingItems = await _clothingItemService.GetUserClothingItemsAsync(CurrentUser.Id); // Repopulate for the view

            var selectedItemsDetails = new List<ClothingItem>();
            if (SelectedItemIds != null && SelectedItemIds.Any())
            {
                foreach (var itemId in SelectedItemIds)
                {
                    var item = AllClothingItems.FirstOrDefault(i => i.Id == itemId);
                    if (item != null)
                    {
                        selectedItemsDetails.Add(item);
                    }
                }
            }
            
            // Construct UserWardrobeContext
            var wardrobeContextBuilder = new StringBuilder();
            foreach (var item in AllClothingItems)
            {
                wardrobeContextBuilder.AppendLine($"- Nome: {item.Name}, Categoria: {item.Category}, Colore: {item.Color}, Stagioni: {item.Seasons ?? "Non specificato"}, Brand: {item.Brand}");
            }

            // Resolve ambiguous reference by fully qualifying the type
            var outfitRequest = new Closy.Services.OutfitGenerationRequest
            {
                UserWardrobeContext = wardrobeContextBuilder.ToString(),
                SelectedItemNames = selectedItemsDetails.Select(i => $"{i.Name} (Categoria: {i.Category}, Colore: {i.Color})").ToList(),
                PreferredColors = SelectedColors ?? new List<string>(),
                PreferredSeasons = SelectedSeasons ?? new List<string>(),
                NumberOfOutfits = 3 // Defaulting to 3, adjust as needed
            };

            _logger.LogInformation("Sending request to GeminiService for outfit generation.");

            // This is the correct place to use the existing IGeminiService
            // to generate outfits based on the user's wardrobe and preferences.
            GeneratedOutfits = await _geminiService.GenerateOutfitsAsync(outfitRequest);

            if (GeneratedOutfits == null || !GeneratedOutfits.Any() || 
                (GeneratedOutfits.Count == 1 && GeneratedOutfits[0].Description != null && 
                 ( (GeneratedOutfits[0].Name != null && GeneratedOutfits[0].Name.Contains("Errore")) || GeneratedOutfits[0].Description.Contains("Errore")) ) )
            {
                ErrorMessage = GeneratedOutfits?.FirstOrDefault()?.Description ?? "Si è verificato un errore durante la generazione dei suggerimenti. Riprova.";
                _logger.LogError("Errore durante la generazione dell'outfit da Gemini: {Error}", ErrorMessage);
                GeneratedOutfits = null; // Clear previous suggestions on error
            }
            else
            {
                 _logger.LogInformation("Suggerimenti generati con successo: {Count} outfits.", GeneratedOutfits.Count);
                 ErrorMessage = null; // Clear previous errors
            }

            return Page();
        }
    }
}
