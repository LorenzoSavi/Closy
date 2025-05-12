using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Closy.Models;
using Closy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Closy.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore; // Added for .Include()

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class OutfitsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClothingItemService _clothingItemService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<OutfitsModel> _logger;

        public OutfitsModel(
            UserManager<ApplicationUser> userManager,
            IClothingItemService clothingItemService,
            ApplicationDbContext dbContext,
            IGeminiService geminiService,
            ILogger<OutfitsModel> logger)
        {
            _userManager = userManager;
            _clothingItemService = clothingItemService;
            _dbContext = dbContext;
            _geminiService = geminiService;
            _logger = logger;
        }

        public ApplicationUser? CurrentUser { get; set; } // Made CurrentUser nullable
        public List<OutfitModel> Outfits { get; set; } = new List<OutfitModel>();
        public List<ClothingItem> AllClothingItems { get; set; } = new List<ClothingItem>();

        // Properties for the modal form
        [BindProperty]
        public List<int> SelectedItemIds { get; set; } = new List<int>(); // Reused from previous form

        [BindProperty]
        public List<string> SelectedModalColors { get; set; } = new List<string>();

        [BindProperty]
        public List<string> SelectedModalSeasons { get; set; } = new List<string>();

        [BindProperty]
        public string? ModalOccasion { get; set; }

        // Data for modal selectors
        public List<(string Hex, string Name)> AvailableColorsForModal { get; set; } = new List<(string Hex, string Name)>();
        public class SeasonInfoModal
        {
            public string Name { get; set; } = "";
            public string[] Icons { get; set; } = Array.Empty<string>();
        }
        public List<SeasonInfoModal> AvailableSeasonInfosForModal { get; set; } = new List<SeasonInfoModal>();

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        private void PopulateModalSelectData()
        {
            AvailableColorsForModal = new List<(string Hex, string Name)>
            {
                ("#000000", "Nero"), ("#FFFFFF", "Bianco"), ("#0047AB", "Blu"),
                ("#DC3545", "Rosso"), ("#28A745", "Verde"), ("#FFC107", "Giallo"),
                ("#6A5ACD", "Viola"), ("#FF69B4", "Rosa"), ("#A52A2A", "Marrone"),
                ("#808080", "Grigio"), ("#F5F5DC", "Beige"), ("#FFA500", "Arancione")
            };

            AvailableSeasonInfosForModal = new List<SeasonInfoModal>
            {
                new SeasonInfoModal { Name = "Primavera", Icons = new[] {"flower1", "cloud-sun", "droplet"} },
                new SeasonInfoModal { Name = "Estate", Icons = new[] {"sun", "thermometer-high", "umbrella"} },
                new SeasonInfoModal { Name = "Autunno", Icons = new[] {"tree", "cloud-rain", "wind"} },
                new SeasonInfoModal { Name = "Inverno", Icons = new[] {"snow", "thermometer-low", "cloud-snow"} }
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return NotFound("User not found.");
            }

            AllClothingItems = await _clothingItemService.GetUserClothingItemsAsync(CurrentUser.Id);
            Outfits = await _dbContext.OutfitModels
                .Where(o => o.UserId == CurrentUser.Id)
                .Include(o => o.Items)
                .OrderByDescending(o => o.Created)
                .ToListAsync();

            PopulateModalSelectData();

            // Pass TempData messages to the properties
            if (TempData.TryGetValue("SuccessMessage", out var successMsg) && successMsg != null)
            {
                SuccessMessage = successMsg.ToString();
            }
            if (TempData.TryGetValue("ErrorMessage", out var errorMsg) && errorMsg != null)
            {
                ErrorMessage = errorMsg.ToString();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateOutfitInModalAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage();
            }

            AllClothingItems = await _clothingItemService.GetUserClothingItemsAsync(CurrentUser.Id);
            PopulateModalSelectData(); 

            // Precondition checks for item counts are removed as per request.
            // We still check if the wardrobe is completely empty.
            if (!AllClothingItems.Any())
            {
                TempData["ErrorMessage"] = "Il tuo guardaroba è vuoto. Aggiungi prima qualche capo per generare outfit.";
                return RedirectToPage();
            }

            string allItemsTextForPrompt = string.Join("\n", AllClothingItems.Select(i =>
                $"- Nome: {i.Name}, Categoria: {i.Category}, Colore: {i.Color}, Stagioni: {i.Seasons ?? "Non specificato"}, Brand: {i.Brand ?? "Non specificato"}"
            ));

            List<ClothingItem> selectedItems = new List<ClothingItem>();
            if (SelectedItemIds != null && SelectedItemIds.Any())
            {
                selectedItems = AllClothingItems.Where(i => SelectedItemIds.Contains(i.Id)).ToList();
            }

            var generationRequest = new Closy.Services.OutfitGenerationRequest
            {
                UserWardrobeContext = allItemsTextForPrompt,
                SelectedItemNames = selectedItems.Select(item => $"{item.Name} ({item.Brand ?? "N/A"}) - Colore: {item.Color ?? "N/A"}, Categoria: {item.Category ?? "N/A"}").ToList(),
                PreferredColors = SelectedModalColors ?? new List<string>(),
                PreferredSeasons = SelectedModalSeasons ?? new List<string>(),
                Occasion = ModalOccasion,
                NumberOfOutfits = 3,
                ForceIncludeSingleItem = selectedItems.Count == 1
            };

            _logger.LogInformation("Sending request to GeminiService (Modal). User: {UserId}, SelectedItems: {ItemsCount}, Colors: {ColorsCount}, Seasons: {SeasonsCount}, Occasion: {Occasion}, NumOutfits: {NumOutfits}",
                CurrentUser.Id,
                generationRequest.SelectedItemNames.Count,
                generationRequest.PreferredColors.Count,
                generationRequest.PreferredSeasons.Count,
                generationRequest.Occasion,
                generationRequest.NumberOfOutfits);

            try
            {
                var generatedOutfits = await _geminiService.GenerateOutfitsAsync(generationRequest);

                if (generatedOutfits != null && generatedOutfits.Any(go => !string.IsNullOrWhiteSpace(go.Name) || !string.IsNullOrWhiteSpace(go.Description)))
                {
                    int savedCount = 0;
                    foreach (var aiOutfit in generatedOutfits)
                    {
                        if (string.IsNullOrWhiteSpace(aiOutfit.Name) && string.IsNullOrWhiteSpace(aiOutfit.Description))
                        {
                            _logger.LogWarning("Skipping AI suggestion with no name and no description for User: {UserId}.", CurrentUser.Id);
                            continue;
                        }

                        var newDbOutfit = new OutfitModel
                        {
                            Name = string.IsNullOrWhiteSpace(aiOutfit.Name) ? "Outfit AI Senza Nome" : aiOutfit.Name,
                            UserId = CurrentUser.Id,
                            Created = DateTime.UtcNow,
                            Tags = string.IsNullOrEmpty(ModalOccasion) ? "AI Generated" : $"AI Generated, {ModalOccasion.Trim()}",
                            Items = new List<ClothingItem>() // Initialize empty list
                        };

                        // Attempt to match and add items based on AI suggestion
                        if (aiOutfit.Top != null)
                        {
                            var matchedTop = FindMatchingClothingItem(aiOutfit.Top.Name, aiOutfit.Top.Brand, aiOutfit.Top.Color);
                            if (matchedTop != null) 
                            {
                                newDbOutfit.Items.Add(matchedTop);
                            }
                            else 
                            {
                                _logger.LogWarning($"MODAL: Could not find matching top item for AI Outfit '{aiOutfit.Name}'. AI: Name='{aiOutfit.Top.Name}', Brand='{aiOutfit.Top.Brand}', Color='{aiOutfit.Top.Color}'. User: {CurrentUser.Id}");
                            }
                        }

                        if (aiOutfit.Pants != null)
                        {
                            var matchedPants = FindMatchingClothingItem(aiOutfit.Pants.Name, aiOutfit.Pants.Brand, aiOutfit.Pants.Color);
                            if (matchedPants != null)
                            {
                                newDbOutfit.Items.Add(matchedPants);
                            }
                            else
                            {
                                _logger.LogWarning($"MODAL: Could not find matching pants item for AI Outfit '{aiOutfit.Name}'. AI: Name='{aiOutfit.Pants.Name}', Brand='{aiOutfit.Pants.Brand}', Color='{aiOutfit.Pants.Color}'. User: {CurrentUser.Id}");
                            }
                        }
                        
                        if (aiOutfit.Shoes != null)
                        {
                            var matchedShoes = FindMatchingClothingItem(aiOutfit.Shoes.Name, aiOutfit.Shoes.Brand, aiOutfit.Shoes.Color);
                            if (matchedShoes != null)
                            {
                                newDbOutfit.Items.Add(matchedShoes);
                            }
                            else
                            {
                                _logger.LogWarning($"MODAL: Could not find matching shoes item for AI Outfit '{aiOutfit.Name}'. AI: Name='{aiOutfit.Shoes.Name}', Brand='{aiOutfit.Shoes.Brand}', Color='{aiOutfit.Shoes.Color}'. User: {CurrentUser.Id}");
                            }
                        }
                        
                        // Save the outfit if at least one item was matched and added
                        if (newDbOutfit.Items.Any())
                        {
                            _dbContext.OutfitModels.Add(newDbOutfit);
                            savedCount++;
                        }
                        else
                        {
                             _logger.LogWarning($"MODAL: AI Outfit '{aiOutfit.Name}' for User {CurrentUser.Id} resulted in no matched items from the wardrobe (Top, Pants, or Shoes were not provided by AI or not matched). Skipping save for this outfit.");
                        }
                    }

                    if (savedCount > 0)
                    {
                        await _dbContext.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"{savedCount} suggeriment{(savedCount == 1 ? "o" : "i")} AI (o part{(savedCount == 1 ? "e" : "i")} di essi) generat{(savedCount == 1 ? "o" : "i")} e salvat{(savedCount == 1 ? "o" : "i")} come nuovi outfit.";
                    }
                    else if (!generatedOutfits.Any(go => !string.IsNullOrWhiteSpace(go.Name) || !string.IsNullOrWhiteSpace(go.Description)))
                    {
                        TempData["ErrorMessage"] = "L'AI ha restituito suggerimenti non validi o vuoti. Nessun outfit è stato salvato.";
                    }
                    else
                    {
                        // This message now means that AI provided suggestions, but none of their items could be matched in the wardrobe.
                        TempData["ErrorMessage"] = "Nessun capo suggerito dall'AI è stato trovato nel tuo guardaroba. Nessun outfit è stato salvato. Controlla i log per maggiori dettagli.";
                    }
                }
                else
                {
                    string errorDetail = generatedOutfits?.FirstOrDefault(go => !string.IsNullOrEmpty(go.Description))?.Description ?? "L'AI non ha generato suggerimenti validi o la risposta era vuota.";
                    TempData["ErrorMessage"] = errorDetail;
                    _logger.LogWarning("MODAL: GeminiService returned no valid named/described outfits for User: {UserId}. Details: {ErrorDetails}", CurrentUser.Id, errorDetail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MODAL: Errore durante la generazione e salvataggio degli outfit con Gemini per User: {UserId}.", CurrentUser.Id);
                TempData["ErrorMessage"] = "Si è verificato un errore critico durante la generazione dei suggerimenti AI. Riprova più tardi.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id) // 'id' is the OutfitId
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                TempData["ErrorMessage"] = "Utente non trovato. Impossibile eliminare l'outfit.";
                return RedirectToPage();
            }

            var outfitToDelete = await _dbContext.OutfitModels
                                       .Include(o => o.Items)
                                       .FirstOrDefaultAsync(o => o.Id == id && o.UserId == CurrentUser.Id);

            if (outfitToDelete == null)
            {
                TempData["ErrorMessage"] = "Outfit non trovato o non sei autorizzato a eliminarlo.";
                return RedirectToPage();
            }

            try
            {
                _dbContext.OutfitModels.Remove(outfitToDelete);
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Outfit eliminato con successo.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Errore database durante l'eliminazione dell'outfit ID {OutfitId} per l'utente {UserId}. Dettagli: {Details}", id, CurrentUser.Id, dbEx.InnerException?.Message);
                TempData["ErrorMessage"] = "Errore durante l'eliminazione dell'outfit. Controlla i log per dettagli.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto durante l'eliminazione dell'outfit ID {OutfitId} per l'utente {UserId}.", id, CurrentUser.Id);
                TempData["ErrorMessage"] = "Si è verificato un errore imprevisto durante l'eliminazione dell'outfit.";
            }

            return RedirectToPage();
        }

        private ClothingItem FindMatchingClothingItem(string name, string brand, string color)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            
            var potentialMatches = AllClothingItems.Where(item => 
                item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (!potentialMatches.Any()) return null;

            if (!string.IsNullOrWhiteSpace(brand))
            {
                var brandMatches = potentialMatches.Where(item => 
                    !string.IsNullOrWhiteSpace(item.Brand) && item.Brand.Equals(brand, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                if (brandMatches.Any()) potentialMatches = brandMatches;
            }

            if (!string.IsNullOrWhiteSpace(color))
            {
                var colorMatches = potentialMatches.Where(item =>
                    !string.IsNullOrWhiteSpace(item.Color) && item.Color.Equals(color, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                if (colorMatches.Any()) potentialMatches = colorMatches;
            }
            
            if (potentialMatches.Count > 1)
            {
                _logger.LogWarning($"Multiple items matched for AI suggestion: Name='{name}', Brand='{brand}', Color='{color}'. Selecting the first one: ID {potentialMatches.First().Id}");
            }

            return potentialMatches.FirstOrDefault();
        }
    }
}