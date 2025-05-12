using System.Collections.Generic;
using System.Threading.Tasks;

namespace Closy.Services
{
    public class OutfitGenerationRequest
    {
        public string UserWardrobeContext { get; set; } 
        public List<string> SelectedItemNames { get; set; } = new List<string>();
        public bool ForceIncludeSingleItem { get; set; } = false; 
        public List<string> PreferredColors { get; set; } = new List<string>();
        public List<string> PreferredSeasons { get; set; } = new List<string>(); 
        public string? Occasion { get; set; }
        public int NumberOfOutfits { get; set; } = 3;
    }

    public class GeneratedOutfit
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        
        public OutfitItem? Top { get; set; }
        public OutfitItem? Pants { get; set; }
        public OutfitItem? Shoes { get; set; }

        public List<string> Items { get; set; } = new List<string>();
    }

    public class OutfitItem
    {
        public string? Name { get; set; }
        public string? Brand { get; set; }
        public string? Color { get; set; }
    }

    public interface IGeminiService
    {
        Task<List<GeneratedOutfit>> GenerateOutfitsAsync(OutfitGenerationRequest outfitRequest);
        Task<string> GenerateTextFromImageAndPromptAsync(byte[] imageBytes, string mimeType, string promptText);
    }
}
