using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Closy.Models;
using Closy.Data;
using Closy.Services;

namespace Closy.Pages.Wardrobe
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly IImageService _imageService;
        private readonly ILogger<CreateModel> _logger;

        public ApplicationUser CurrentUser { get; set; }
        public int TotalItems { get; set; }
        public int SavedOutfits { get; set; }
        public int FavoriteItems { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        // Definizione della classe InputModel all'interno di CreateModel
        public class InputModel
        {
            [Required(ErrorMessage = "Il nome è obbligatorio")]
            public string Name { get; set; }

            [Required(ErrorMessage = "La marca è obbligatoria")]
            public string Brand { get; set; }

            [Required(ErrorMessage = "La categoria è obbligatoria")]
            public string Category { get; set; }

            [Required(ErrorMessage = "Il colore è obbligatorio")]
            public string Color { get; set; }
        }

        public CreateModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext,
            IImageService imageService,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                CurrentUser = await _userManager.GetUserAsync(User);
                if (CurrentUser == null)
                {
                    return NotFound();
                }

                // Popola i contatori in modo sicuro
                TotalItems = await _dbContext.ClothingItems
                    .CountAsync(i => i.UserId == CurrentUser.Id);

                FavoriteItems = await _dbContext.ClothingItems
                    .CountAsync(i => i.UserId == CurrentUser.Id && i.IsFavorite);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il caricamento della pagina Create");
                // In caso di errore, mostra comunque la pagina con contatori a 0
                TotalItems = 0;
                SavedOutfits = 0;
                FavoriteItems = 0;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(IFormFile Image, string[] seasons)
        {
            try
            {
                CurrentUser = await _userManager.GetUserAsync(User);
                if (CurrentUser == null)
                {
                    return NotFound();
                }

                if (Image == null || Image.Length == 0)
                {
                    ModelState.AddModelError("Image", "L'immagine è obbligatoria");
                }

                if (seasons == null || seasons.Length == 0)
                {
                    ModelState.AddModelError("seasons", "Seleziona almeno una stagione");
                }

                if (!ModelState.IsValid)
                {
                    // Ricarica i contatori per la sidebar
                    TotalItems = await _dbContext.ClothingItems.CountAsync(i => i.UserId == CurrentUser.Id);
                    FavoriteItems = await _dbContext.ClothingItems.CountAsync(i => i.UserId == CurrentUser.Id && i.IsFavorite);
                    return Page();
                }

                // Salva l'immagine
                string originalImagePath = await _imageService.SaveImageAsync(Image);
                _logger.LogInformation($"Immagine salvata in: {originalImagePath}");

                // Crea il nuovo capo
                var clothingItem = new ClothingItem
                {
                    Name = Input.Name,
                    Brand = Input.Brand,
                    Category = Input.Category,
                    Color = Input.Color,
                    UserId = CurrentUser.Id,
                    ImageUrl = originalImagePath,
                    OriginalImageUrl = originalImagePath,
                    Seasons = string.Join(",", seasons),
                    CreatedAt = DateTime.UtcNow,
                    IsFavorite = false
                };

                // Salva nel database
                await _dbContext.ClothingItems.AddAsync(clothingItem);
                await _dbContext.SaveChangesAsync();

                TempData["Message"] = "Capo aggiunto con successo!";
                return RedirectToPage("./AllItems");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio del capo");
                ModelState.AddModelError("", "Si è verificato un errore durante il salvataggio. Riprova.");

                // Ricarica i contatori per la sidebar
                TotalItems = await _dbContext.ClothingItems.CountAsync(i => i.UserId == CurrentUser.Id);
                FavoriteItems = await _dbContext.ClothingItems.CountAsync(i => i.UserId == CurrentUser.Id && i.IsFavorite);

                return Page();
            }
        }
    }
}