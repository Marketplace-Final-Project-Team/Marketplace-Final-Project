using Marketplace.BLL.DTOs.Gemini;
using Marketplace.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Marketplace.BLL.Services
{
    public class GeminiChatApiClient: IGeminiChatApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ISystemSettingsService _settingsService;
        private readonly ILogger<GeminiChatApiClient> _logger;

        public GeminiChatApiClient(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ISystemSettingsService settingsService, 
            ILogger<GeminiChatApiClient> logger
            )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _settingsService = settingsService;
            _logger = logger;
        }

        public async Task<GeminiResponse> GenerateContentAsync(List<GeminiContent> contents, object[] toolDeclarations, string systemInstruction)
        {
            var apiKey = await _settingsService.GetAsync("GoogleAI", "ApiKey") 
                ?? _configuration["GoogleAI:ApiKey"] 
                ?? throw new InvalidOperationException("GoogleAI ApiKey is not configured.");

            var model = await _settingsService.GetAsync("GoogleAI", "Model")
                ?? _configuration["GoogleAI:Model"]
                ?? "gemini-3.6-flash";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                contents = contents,

                tools = new object[]
                {
                    new { function_declarations = toolDeclarations }
                },
                system_instruction = new 
                {
                    parts = new[] { new { text = systemInstruction } }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Gemini API returned {StatusCode} for model {Model} (chat): {ErrorBody}", response.StatusCode, model, errorBody);
            }
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

            if(geminiResponse == null)
            {
                _logger.LogError("Failed to deserialize Gemini API response for model Gemini API (chat)");
                throw new InvalidOperationException("Failed to deserialize Gemini API response.");
            }

            return geminiResponse;
        }
    }
}
