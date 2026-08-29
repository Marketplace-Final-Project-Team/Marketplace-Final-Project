using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }
}
