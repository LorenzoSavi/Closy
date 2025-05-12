using Closy.Models;

namespace Closy.Services
{
    public interface IClothingItemService
    {
        Task<List<ClothingItem>> GetUserClothingItemsAsync(string userId);
        Task<ClothingItem> GetClothingItemByIdAsync(int id);
        Task<ClothingItem> AddClothingItemAsync(ClothingItem item);
        Task<ClothingItem> UpdateClothingItemAsync(ClothingItem item);
        Task DeleteClothingItemAsync(int id);
        Task<bool> ToggleFavoriteAsync(int id);


        // Metodi per la validazione
        Task<bool> HasMinimumRequiredItemsAsync(string userId, Dictionary<string, int> requirements);

        // Metodi per le statistiche
        Task<Dictionary<string, int>> GetUserClothingStatsAsync(string userId);
        Task<int> GetTotalItemsCountAsync(string userId);
        Task<int> GetFavoriteItemsCountAsync(string userId);
    }
}