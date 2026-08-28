using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }
}
