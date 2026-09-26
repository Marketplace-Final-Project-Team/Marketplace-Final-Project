using Marketplace.BLL.DTOs.Gemini;

namespace Marketplace.BLL.Interfaces
{
    public interface IGeminiChatApiClient
    {
        Task<GeminiResponse> GenerateContentAsync(List<GeminiContent> contents, object[] toolDeclarations, string systemInstruction);
    }
}