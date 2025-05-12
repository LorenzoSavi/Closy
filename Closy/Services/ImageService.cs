using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp; // Usa l'alias per evitare ambiguità
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Closy.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;

        public ImageService(IWebHostEnvironment environment, ILogger<ImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveImageAsync(IFormFile image)
        {
            try
            {
                _logger.LogInformation($"Inizio salvataggio immagine: {image.FileName}");

                // Verifica che la directory wwwroot esista
                if (!Directory.Exists(_environment.WebRootPath))
                {
                    _logger.LogError($"WebRootPath non trovato: {_environment.WebRootPath}");
                    Directory.CreateDirectory(_environment.WebRootPath);
                }

                // Crea la directory uploadsPhoto se non esiste
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploadsPhoto"); // Cambiato qui
                _logger.LogInformation($"Percorso cartella uploadsPhoto: {uploadsFolder}");

                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Creazione cartella uploadsPhoto...");
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Genera un nome file unico
                var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                _logger.LogInformation($"Salvataggio file in: {filePath}");

                // Salva il file direttamente
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var relativePath = $"/uploadsPhoto/{uniqueFileName}"; // Cambiato qui
                _logger.LogInformation($"File salvato con successo. Percorso relativo: {relativePath}");

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio dell'immagine");
                throw;
            }
        }

        public async Task<string> RemoveBackgroundAsync(string imagePath)
        {
            try
            {
                // Per ora, restituisci semplicemente l'immagine originale
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la rimozione dello sfondo");
                throw;
            }
        }
    }
}