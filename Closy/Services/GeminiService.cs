using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // Required for JsonPropertyName
using System.Threading.Tasks;
using System.Collections.Generic; // Required for List<Part>
using System.Linq; // Required for LINQ operations
using System.IO; // Required for Stream and MemoryStream
using System.Buffers.Text; // Required for Base64 encoding, if not using Convert.ToBase64String

namespace Closy.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = "AIzaSyCSnaJcfMGYtw-wLlWDKwGyPmJ0-jJN2EA"; // Updated API Key
            _apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent"; // Updated API Endpoint

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Gemini API Key is not configured in appsettings.json (GeminiAI:ApiKey).");
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }
        }

        // Define nested classes for the request structure
        private class GeminiRequest
        {
            public List<Content> contents { get; set; }
        }

        private class Content
        {
            public List<Part> parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string text { get; set; }

            [JsonPropertyName("inlineData")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public InlineData inlineData { get; set; }
        }

        // New class for inline image data
        private class InlineData
        {
            [JsonPropertyName("mime_type")]
            public string mime_type { get; set; }

            [JsonPropertyName("data")]
            public string data { get; set; } // Base64 encoded image data
        }

        // Define nested classes for the response structure (simplified)
        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate> candidates { get; set; }
            [JsonPropertyName("promptFeedback")]
            public PromptFeedback promptFeedback { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content content { get; set; }
            [JsonPropertyName("finishReason")]
            public string finishReason { get; set; }
            [JsonPropertyName("index")]
            public int index { get; set; }
            [JsonPropertyName("safetyRatings")]
            public List<SafetyRating> safetyRatings { get; set; }
        }

        private class SafetyRating
        {
            [JsonPropertyName("category")]
            public string category { get; set; }
            [JsonPropertyName("probability")]
            public string probability { get; set; }
        }

        private class PromptFeedback
        {
            [JsonPropertyName("blockReason")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string BlockReason { get; set; }

            [JsonPropertyName("safetyRatings")]
            public List<SafetyRating> safetyRatings { get; set; }
        }

        public async Task<List<GeneratedOutfit>> GenerateOutfitsAsync(OutfitGenerationRequest outfitRequest)
        {
            if (outfitRequest == null || string.IsNullOrEmpty(outfitRequest.UserWardrobeContext))
            {
                _logger.LogWarning("OutfitGenerationRequest or UserWardrobeContext is null or empty.");
                return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore Richiesta", Description = "Richiesta di generazione outfit non valida o guardaroba vuoto." } };
            }

            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Sei un personal stylist AI. Il tuo compito è generare proposte di outfit uniche, alla moda e sensate per un utente, basandoti sul suo guardaroba e sulle sue preferenze.");
            promptBuilder.AppendLine("\nGuardaroba completo dell'utente (ogni riga è un capo disponibile con i suoi dettagli: Nome, Categoria, Colore, Stagioni, Brand):");
            promptBuilder.AppendLine(outfitRequest.UserWardrobeContext);

            promptBuilder.AppendLine("\nPreferenze e selezioni dell'utente (opzionali):");
            if (outfitRequest.SelectedItemNames.Any())
            {
                if (outfitRequest.ForceIncludeSingleItem && outfitRequest.SelectedItemNames.Count == 1)
                {
                    promptBuilder.AppendLine($"- L'utente ha specificato che l'outfit DEVE OBBLIGATORIAMENTE includere il seguente capo: {outfitRequest.SelectedItemNames.First()}. Assicurati che questo capo sia presente in OGNI outfit generato, rispettando comunque la necessità di un top, un pantalone e scarpe.");
                }
                else
                {
                    promptBuilder.AppendLine($"- L'utente vorrebbe includere alcuni dei seguenti capi, se possibile: {string.Join("; ", outfitRequest.SelectedItemNames)}.");
                }
            }
            else
            {
                promptBuilder.AppendLine("- L'utente non ha pre-selezionato capi specifici.");
            }

            if (outfitRequest.PreferredColors.Any())
            {
                promptBuilder.AppendLine($"- L'utente ha espresso preferenza per i seguenti colori (o vuole includerli): {string.Join(", ", outfitRequest.PreferredColors)}.");
            }
            else
            {
                promptBuilder.AppendLine("- L'utente non ha specificato preferenze di colore.");
            }

            if (outfitRequest.PreferredSeasons.Any())
            {
                promptBuilder.AppendLine($"- Adatta gli outfit per le seguenti stagioni: {string.Join(", ", outfitRequest.PreferredSeasons)}.");
            }
            else
            {
                promptBuilder.AppendLine("- L'utente non ha specificato preferenze di stagione.");
            }
            
            if (!string.IsNullOrEmpty(outfitRequest.Occasion))
            {
                 promptBuilder.AppendLine($"- L'occasione specificata è: {outfitRequest.Occasion}.");
            }
            else
            {
                promptBuilder.AppendLine("- L'utente non ha specificato un'occasione; proponi outfit adatti a contesti generici o vari.");
            }

            promptBuilder.AppendLine($"\nIstruzioni FONDAMENTALI per la generazione degli {outfitRequest.NumberOfOutfits} outfit:");
            promptBuilder.AppendLine($"- Genera ESATTAMENTE {outfitRequest.NumberOfOutfits} proposte di outfit COMPLETAMENTE DIVERSI tra loro. Se il guardaroba è limitato e non puoi garantire completa diversità per tutti i capi, fai del tuo meglio per variare almeno un capo significativo (es. top o pantaloni) tra gli outfit.");
            promptBuilder.AppendLine("- Ogni outfit DEVE essere composto ESCLUSIVAMENTE da capi presenti nel 'Guardaroba completo dell'utente' fornito. Non inventare o modificare i capi.");
            promptBuilder.AppendLine("- Ogni outfit DEVE OBBLIGATORIAMENTE includere ESATTAMENTE:");
            promptBuilder.AppendLine("    1. Un (1) capo superiore (es. maglia, camicia, felpa).");
            promptBuilder.AppendLine("    2. Un (1) paio di pantaloni (o capo inferiore equivalente come gonna).");
            promptBuilder.AppendLine("    3. Un (1) paio di scarpe.");
            promptBuilder.AppendLine($"- MASSIMIZZA LA VARIETÀ: Assicurati che ogni outfit sia il più diverso possibile dagli altri, usando combinazioni di colori e stili differenti.");
            promptBuilder.AppendLine("- Non ripetere MAI la stessa combinazione esatta di top, pantaloni e scarpe in outfit diversi.");
            promptBuilder.AppendLine("- Presta MOLTA attenzione agli abbinamenti di colore: devono essere armoniosi e sensati.");
            
            promptBuilder.AppendLine("- Per OGNI outfit generato, DEVI fornire OBBLIGATORIAMENTE:");
            promptBuilder.AppendLine("    a) Un nome creativo e univoco per l'outfit (proprietà 'name').");
            promptBuilder.AppendLine("    b) Una breve descrizione (1-2 frasi) che ne spieghi lo stile, l'occasione d'uso e la logica dietro gli abbinamenti (proprietà 'description').");
            promptBuilder.AppendLine("    c) Le informazioni dettagliate sui capi, suddivisi per categoria in strutture specifiche (DEVI popolare questi oggetti, non lasciarli nulli):");
            promptBuilder.AppendLine("       - 'top': oggetto con proprietà 'name', 'brand' e 'color'.");
            promptBuilder.AppendLine("       - 'pants': oggetto con proprietà 'name', 'brand' e 'color'.");
            promptBuilder.AppendLine("       - 'shoes': oggetto con proprietà 'name', 'brand' e 'color'.");
            promptBuilder.AppendLine("       IMPORTANTE: Per ciascun capo (top, pants, shoes), devi selezionare un articolo specifico dal 'Guardaroba completo dell'utente'. Le proprietà 'name', 'brand' e 'color' nell'oggetto JSON DEVONO CORRISPONDERE ESATTAMENTE al 'Nome', 'Brand' e 'Colore' dell'articolo scelto dal guardaroba. Se il 'Brand' per un articolo nel guardaroba è 'Non specificato' o mancante, usa 'N/A' o ometti la proprietà 'brand' nel JSON. Fai lo stesso per 'color' se non specificato.");
            
            promptBuilder.AppendLine("- Assicurati che gli abbinamenti siano coerenti per stile e stagione (se specificata).");
            promptBuilder.AppendLine("- Formatta l'intera risposta come un singolo array JSON. Ogni elemento dell'array è un oggetto JSON che rappresenta un outfit.");
            promptBuilder.AppendLine("\nEsempio di formato per un singolo outfit nell'array JSON:");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("  \"name\": \"Casual Chic per il Weekend\",");
            promptBuilder.AppendLine("  \"description\": \"Un look rilassato ma stiloso, perfetto per uscite informali nel fine settimana. Abbina il blu dei jeans con il grigio neutro del maglione.\",");
            promptBuilder.AppendLine("  \"top\": {");
            promptBuilder.AppendLine("    \"name\": \"Maglione a Collo Alto\",");
            promptBuilder.AppendLine("    \"brand\": \"Zara\",");
            promptBuilder.AppendLine("    \"color\": \"Grigio\"");
            promptBuilder.AppendLine("  },");
            promptBuilder.AppendLine("  \"pants\": {");
            promptBuilder.AppendLine("    \"name\": \"Jeans Slim Fit\",");
            promptBuilder.AppendLine("    \"brand\": \"Levi's\",");
            promptBuilder.AppendLine("    \"color\": \"Blu Scuro\"");
            promptBuilder.AppendLine("  },");
            promptBuilder.AppendLine("  \"shoes\": {");
            promptBuilder.AppendLine("    \"name\": \"Sneakers Basse\",");
            promptBuilder.AppendLine("    \"brand\": \"Adidas\",");
            promptBuilder.AppendLine("    \"color\": \"Bianco\"");
            promptBuilder.AppendLine("  }");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("\nNon usare markdown nella risposta JSON, solo il JSON puro come array.");

            string prompt = promptBuilder.ToString();
            _logger.LogInformation("Sending prompt to Gemini API. Endpoint: {Endpoint}. Prompt: {Prompt}", _apiEndpoint, prompt);

            try
            {
                var requestPayload = new GeminiRequest
                {
                    contents = new List<Content>
                    {
                        new Content
                        {
                            parts = new List<Part> { new Part { text = prompt } }
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(requestPayload);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiEndpoint}?key={_apiKey}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully received response from Gemini API. Response body: {ResponseBody}", responseBody);
                    try
                    {
                        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
                        if (geminiResponse?.candidates != null && geminiResponse.candidates.Any() &&
                            geminiResponse.candidates[0].content?.parts != null && geminiResponse.candidates[0].content.parts.Any())
                        {
                            var textResponse = geminiResponse.candidates[0].content.parts[0].text.Trim();
                            // Ensure the response is treated as an array of JSON objects
                            if (textResponse.StartsWith("```json"))
                            {
                                textResponse = textResponse.Substring(7, textResponse.Length - 7 - 3).Trim();
                            }
                            else if (textResponse.StartsWith("```"))
                            {
                                textResponse = textResponse.Substring(3, textResponse.Length - 3 - 3).Trim();
                            }

                            var parsedOutfits = JsonSerializer.Deserialize<List<GeneratedOutfit>>(textResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (parsedOutfits != null)
                            {
                                var uniqueOutfits = new List<GeneratedOutfit>();
                                var usedItemSignatures = new HashSet<string>();
                                
                                foreach (var outfit in parsedOutfits)
                                {
                                    // Create a signature based on the names of the top, pants, and shoes.
                                    // Ensure Top, Pants, Shoes, and their Name properties are not null before accessing.
                                    string topName = outfit.Top?.Name ?? "N/A_Top";
                                    string pantsName = outfit.Pants?.Name ?? "N/A_Pants";
                                    string shoesName = outfit.Shoes?.Name ?? "N/A_Shoes";
                                    var outfitSignature = $"{topName}|{pantsName}|{shoesName}";

                                    // An outfit is considered valid for adding if it has a signature 
                                    // AND (it has a name OR description)
                                    // AND (it has all three components: top, pants, shoes)
                                    bool hasContent = !string.IsNullOrWhiteSpace(outfit.Name) || !string.IsNullOrWhiteSpace(outfit.Description);
                                    bool hasAllItems = outfit.Top != null && outfit.Pants != null && outfit.Shoes != null &&
                                                       !string.IsNullOrWhiteSpace(outfit.Top.Name) &&
                                                       !string.IsNullOrWhiteSpace(outfit.Pants.Name) &&
                                                       !string.IsNullOrWhiteSpace(outfit.Shoes.Name);

                                    if (!string.IsNullOrWhiteSpace(outfitSignature) && 
                                        !usedItemSignatures.Contains(outfitSignature) &&
                                        hasContent && hasAllItems)
                                    {
                                        usedItemSignatures.Add(outfitSignature);
                                        uniqueOutfits.Add(outfit);
                                    }
                                    else if (!hasContent || !hasAllItems)
                                    {
                                        _logger.LogWarning("Skipping an AI outfit due to missing name/description or missing top/pants/shoes details. Outfit Name: {OutfitName}", outfit.Name ?? "N/A");
                                    }
                                }
                                
                                if (!uniqueOutfits.Any() && parsedOutfits.Any())
                                {
                                    _logger.LogWarning("All parsed AI outfits were filtered out by uniqueness or content validation. Original count: {ParsedCount}", parsedOutfits.Count);
                                }
                                return uniqueOutfits;
                            }
                        }
                        _logger.LogWarning("Gemini API response was successful but content was not in the expected List<GeneratedOutfit> format or standard GeminiResponse format. Response: {ResponseBody}", responseBody);
                        return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore Deserializzazione", Description = "La risposta dell'AI non è nel formato atteso o è vuota." } };
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization exception when processing Gemini API response. ResponseBody: {ResponseBody}", responseBody);
                        return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore JSON", Description = $"Errore durante la lettura della risposta dell'AI: {jsonEx.Message}. Risposta ricevuta: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}" } };
                    }
                }
                else
                {
                     var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from Gemini API: {StatusCode} - {ErrorBody}", response.StatusCode, errorContent);
                    return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore API", Description = $"Errore dall'API Gemini: {response.StatusCode}. Dettagli: {errorContent}" } };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception when calling Gemini API.");
                return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore HTTP", Description = $"Eccezione HTTP: {ex.Message}" } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception when calling Gemini API.");
                return new List<GeneratedOutfit> { new GeneratedOutfit { Name = "Errore Inaspettato", Description = $"Eccezione: {ex.Message}" } };
            }
        }

        public async Task<string> GenerateTextFromImageAndPromptAsync(byte[] imageBytes, string mimeType, string promptText)
        {
            _logger.LogInformation("Attempting to generate text from image and prompt. Prompt: {PromptText}, Image MIME type: {MimeType}, Image size: {ImageSize} bytes", promptText, mimeType, imageBytes.Length);

            if (string.IsNullOrEmpty(promptText) && (imageBytes == null || imageBytes.Length == 0))
            {
                _logger.LogWarning("Both prompt text and image data are empty.");
                return "Prompt text and image data cannot both be empty.";
            }
            if (imageBytes != null && string.IsNullOrEmpty(mimeType))
            {
                _logger.LogWarning("Image data provided without a MIME type.");
                return "Image MIME type must be provided if image data is present.";
            }

            var parts = new List<Part>();
            if (!string.IsNullOrEmpty(promptText))
            {
                parts.Add(new Part { text = promptText });
            }
            if (imageBytes != null && imageBytes.Length > 0)
            {
                parts.Add(new Part
                {
                    inlineData = new InlineData
                    {
                        mime_type = mimeType,
                        data = Convert.ToBase64String(imageBytes)
                    }
                });
            }

            var requestPayload = new GeminiRequest
            {
                contents = new List<Content>
                {
                    new Content { parts = parts }
                }
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiEndpoint}?key={_apiKey}");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending multimodal request to Gemini API. Endpoint: {Endpoint}", _apiEndpoint);

                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully received response from Gemini API for image and prompt.");
                    try
                    {
                        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
                        if (geminiResponse?.candidates != null && geminiResponse.candidates.Any() &&
                            geminiResponse.candidates[0].content?.parts != null && geminiResponse.candidates[0].content.parts.Any() &&
                            !string.IsNullOrEmpty(geminiResponse.candidates[0].content.parts[0].text))
                        {
                            return geminiResponse.candidates[0].content.parts[0].text.Trim();
                        }
                        else if (geminiResponse?.promptFeedback?.BlockReason != null)
                        {
                            var blockReason = geminiResponse.promptFeedback.BlockReason;
                            var safetyRatings = string.Join(", ", geminiResponse.promptFeedback.safetyRatings?.Select(r => $"{r.category}: {r.probability}") ?? Enumerable.Empty<string>());
                            _logger.LogWarning("Gemini API blocked the prompt. Reason: {BlockReason}, Safety Ratings: {SafetyRatings}", blockReason, safetyRatings);
                            return $"Content generation blocked by API. Reason: {blockReason}.";
                        }
                        _logger.LogWarning("Gemini API response was successful but content was not in the expected format or was empty. Response: {ResponseBody}", responseBody.Substring(0, Math.Min(responseBody.Length, 1000)));
                        return "Failed to extract a valid description from the API response.";
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "JSON deserialization exception when processing Gemini API response for image and prompt. ResponseBody: {ResponseBody}", responseBody.Substring(0, Math.Min(responseBody.Length, 1000)));
                        return $"Error parsing the API response: {jsonEx.Message}.";
                    }
                }
                else
                {
                    _logger.LogError("Error from Gemini API (image and prompt): {StatusCode} - {ErrorBody}", response.StatusCode, responseBody);
                    return $"API error: {response.StatusCode}. Details: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}";
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception when calling Gemini API for image and prompt.");
                return $"HTTP request error: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception when calling Gemini API for image and prompt.");
                return $"An unexpected error occurred: {ex.Message}";
            }
        }
    }
}
