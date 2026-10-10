using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountForBuyerAsync(string userId);
        Task<List<NotificationItemDto>> GetRecentNotificationsAsync(string userId, int take = 15);
        Task MarkOrderViewedAsync(string userId, Guid orderId);
    }
}