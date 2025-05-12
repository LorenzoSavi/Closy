using Closy.Models;
using Closy.Data;
using Microsoft.EntityFrameworkCore;

namespace Closy.Services
{
    public class ClothingItemService : IClothingItemService
    {
        private readonly ApplicationDbContext _context;

        public ClothingItemService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClothingItem>> GetUserClothingItemsAsync(string userId)
        {
            return await _context.ClothingItems
                .Where(item => item.UserId == userId)
                .ToListAsync();
        }

        // Metodo mancante implementato
        public async Task<ClothingItem> GetClothingItemByIdAsync(int id)
        {
            return await _context.ClothingItems.FindAsync(id);
        }

        // Metodo mancante implementato
        public async Task DeleteClothingItemAsync(int id)
        {
            var item = await _context.ClothingItems.FindAsync(id);
            if (item != null)
            {
                _context.ClothingItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }


        // Implementa anche questi metodi richiesti dall'interfaccia
        public async Task<ClothingItem> AddClothingItemAsync(ClothingItem item)
        {
            _context.ClothingItems.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<ClothingItem> UpdateClothingItemAsync(ClothingItem item)
        {
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> ToggleFavoriteAsync(int id)
        {
            var item = await _context.ClothingItems.FindAsync(id);
            if (item != null)
            {
                item.IsFavorite = !item.IsFavorite;
                await _context.SaveChangesAsync();
                return item.IsFavorite;
            }
            return false;
        }

        public async Task<bool> HasMinimumRequiredItemsAsync(string userId, Dictionary<string, int> requirements)
        {
            var items = await GetUserClothingItemsAsync(userId);
            foreach (var requirement in requirements)
            {
                var count = items.Count(i => i.Category == requirement.Key);
                if (count < requirement.Value)
                    return false;
            }
            return true;
        }

        public async Task<Dictionary<string, int>> GetUserClothingStatsAsync(string userId)
        {
            var items = await GetUserClothingItemsAsync(userId);
            return items.GroupBy(i => i.Category)
                       .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<int> GetTotalItemsCountAsync(string userId)
        {
            return await _context.ClothingItems
                .CountAsync(i => i.UserId == userId);
        }

        public async Task<int> GetFavoriteItemsCountAsync(string userId)
        {
            return await _context.ClothingItems
                .CountAsync(i => i.UserId == userId && i.IsFavorite);
        }
    }
}