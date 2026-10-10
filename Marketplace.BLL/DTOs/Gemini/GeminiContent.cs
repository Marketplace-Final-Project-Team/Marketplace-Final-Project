using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiContent
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }
}
