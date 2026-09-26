namespace Marketplace.BLL.Interfaces
{
    public interface IGeminiChatService
    {
        Task<string> SendMessageAsync(string userMessage);
    }
}
