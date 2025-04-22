using Microsoft.AspNetCore.Mvc;
using Closy.Data;
using Microsoft.EntityFrameworkCore;

namespace Closy.Pages.UserProfiles.Interno.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClothingItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClothingItemsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClothingItem(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Unauthorized();

            var clothingItem = await _context.ClothingItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserProfileId == userId.Value);

            if (clothingItem == null)
                return NotFound();

            _context.ClothingItems.Remove(clothingItem);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}