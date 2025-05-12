public interface IImageService
{
    Task<string> SaveImageAsync(IFormFile image);
    Task<string> RemoveBackgroundAsync(string imagePath);
}