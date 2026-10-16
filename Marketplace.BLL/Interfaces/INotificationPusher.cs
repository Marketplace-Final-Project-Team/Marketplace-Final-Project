namespace Marketplace.BLL.Interfaces
{
    public interface INotificationPusher
    {
        Task PushToUserAsync(string userId, string text, string type);
    }
}
