namespace Closy.Models
{
    public class OutfitModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Tags { get; set; }
        public DateTime Created { get; set; }
        public bool IsAIGenerated { get; set; }
        public string? Description { get; set; }
        public List<ClothingItem> Items { get; set; } = new List<ClothingItem>();
        public string? UserId { get; set; }
    }
}