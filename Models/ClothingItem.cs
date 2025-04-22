using System.ComponentModel.DataAnnotations;

namespace Closy.Models
{
    public class ClothingItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Il nome è obbligatorio")]
        public string Name { get; set; }

        [Required(ErrorMessage = "La categoria è obbligatoria")]
        public string Category { get; set; }

        public string? ImagePath { get; set; }

        public int UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; }
    }
}