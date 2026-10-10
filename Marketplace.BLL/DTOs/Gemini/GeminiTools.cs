using System.Text.Json.Serialization;

namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiTools
    {
        [JsonPropertyName("function_declarations")]
        public object[] FunctionDeclarations { get; set; } = null!;
    }
}