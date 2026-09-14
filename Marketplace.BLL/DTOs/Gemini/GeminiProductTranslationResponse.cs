namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiProductTranslationResponse
    {
        public GeminiProductTranslationEntry En { get; set; } = new();
        public GeminiProductTranslationEntry De { get; set; } = new();
        public GeminiProductTranslationEntry Fr { get; set; } = new();
        public GeminiProductTranslationEntry Es { get; set; } = new();
    }

    public class GeminiProductTranslationEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
