using System;
using System.Collections.Generic;

namespace Closy.Models
{
    public class GeneratedOutfit
    {
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Structured properties for outfit items
        public OutfitItem Top { get; set; }
        public OutfitItem Pants { get; set; }
        public OutfitItem Shoes { get; set; }
        
        // Original Items array for backward compatibility
        public string[] Items { get; set; }
    }

    public class OutfitItem
    {
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Color { get; set; }
    }
}
