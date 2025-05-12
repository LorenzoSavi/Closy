using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Closy.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string Nome { get; set; }

        [Required]
        [StringLength(50)]
        public string Cognome { get; set; }

        public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;
        
        public string? ProfilePictureUrl { get; set; }

        public ICollection<ClothingItem> ClothingItems { get; set; } = new List<ClothingItem>();
        public ICollection<OutfitModel> Outfits { get; set; } = new List<OutfitModel>();

        // Preferences
        public string? PreferredCategories { get; set; } // JSON string or comma-separated
        public string? PreferredColors { get; set; }     // JSON string or comma-separated
        public string? PreferredStyles { get; set; }     // JSON string or comma-separated
        public string? Sizes { get; set; }               // JSON string for different item types e.g., {"shirt": "M", "pants": "32"}

        public DateTime? LastLoginDate { get; set; } // Ensure this property exists
    }
}