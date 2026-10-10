using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("functionCall")]
        public GeminiFunctionCall? FunctionCall { get; set; }

        [JsonPropertyName("functionResponse")]
        public GeminiFunctionResponse? FunctionResponse { get; set; }

        [JsonPropertyName("thoughtSignature")]
        public string? ThoughtSignature { get; set; }
    }
}
