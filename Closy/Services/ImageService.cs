using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp; // Usa l'alias per evitare ambiguità
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Closy.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageService(
            IWebHostEnvironment environment, 
            ILogger<ImageService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
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
                _logger.LogInformation($"Inizio rimozione sfondo per: {imagePath}");
                
                // Ottiene l'API key da appsettings.json
                string apiKey = _configuration["RemoveBg:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("RemoveBg API Key non trovata in configurazione");
                    return imagePath; // Ritorna l'immagine originale se manca la chiave API
                }

                // Crea il percorso fisico dell'immagine
                string webRootPath = _environment.WebRootPath;
                string filePath = Path.Combine(webRootPath, imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File non trovato: {filePath}");
                    return imagePath;
                }

                // Crea un nuovo nome file per l'immagine senza sfondo
                string directory = Path.GetDirectoryName(filePath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                string newFileName = $"{fileNameWithoutExt}_no_bg{extension}";
                string outputPath = Path.Combine(directory, newFileName);
                
                // Crea l'URL relativo per il nuovo file
                string relativePath = imagePath.Substring(0, imagePath.LastIndexOf('/') + 1) + newFileName;

                // Chiama l'API remove.bg
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                using var formData = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // Imposta il tipo di contenuto appropriato
                formData.Add(fileContent, "image_file", Path.GetFileName(filePath));

                var response = await httpClient.PostAsync("https://api.remove.bg/v1.0/removebg", formData);
                
                if (response.IsSuccessStatusCode)
                {
                    using var outputStream = new FileStream(outputPath, FileMode.Create);
                    await response.Content.CopyToAsync(outputStream);
                    _logger.LogInformation($"Sfondo rimosso con successo: {relativePath}");
                    return relativePath;
                }
                else
                {
                    _logger.LogWarning($"Errore nell'API remove.bg: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return imagePath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la rimozione dello sfondo");
                return imagePath; // In caso di errore, ritorna l'immagine originale
            }
        }
    }
}