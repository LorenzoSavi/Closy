using System.Collections.Generic;

namespace Closy.Models
{
    public class OutfitGenerationRequest
    {
        public string UserWardrobeContext { get; set; }
        public List<string> SelectedItemNames { get; set; } = new List<string>();
        public List<string> PreferredColors { get; set; } = new List<string>();
        public List<string> PreferredSeasons { get; set; } = new List<string>();
        public string Occasion { get; set; }
        public int NumberOfOutfits { get; set; } = 3;
        public bool ForceIncludeSingleItem { get; set; }
    }
}
