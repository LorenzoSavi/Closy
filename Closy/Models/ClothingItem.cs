using System;

namespace Closy.Models
{
    public class ClothingItem
    {
        public int Id { get; set; }

        public string? UserId { get; set; }  // Per collegare il capo all'utente

        public string? Name { get; set; }

        public string? Brand { get; set; }

        public string? Category { get; set; }

        public string? Color { get; set; }

        public string? Seasons { get; set; }

        public string? Season { get; set; }  // Aggiunta per compatibilità con Details.cshtml

        public string? ImageUrl { get; set; }

        public string? OriginalImageUrl { get; set; }  // Per mantenere l'immagine originale

        public bool IsFavorite { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PurchaseDate { get; set; }  // Aggiunta per compatibilità con Details.cshtml

        public string? Notes { get; set; }  // Aggiunta per compatibilità con Details.cshtml

        public DateTime DateAdded { get; set; } = DateTime.Now;  // Aggiunta per compatibilità con Details.cshtml

        // Riferimento all'utente proprietario
        public virtual ApplicationUser? User { get; set; }
    }
}