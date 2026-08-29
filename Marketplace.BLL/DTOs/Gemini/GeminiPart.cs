using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
