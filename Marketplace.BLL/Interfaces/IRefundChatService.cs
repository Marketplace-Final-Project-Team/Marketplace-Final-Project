using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IRefundChatService
    {
        Task<RefundChatMessageDto?> SaveMessageAsync(Guid postSaleRequestId, string senderId, bool isFromAdmin, bool isSellerChannel, string text);
        Task<List<RefundChatMessageDto>> GetMessagesAsync(Guid postSaleRequestId, bool isSellerChannel);
        Task<List<RefundChatThreadDto>> GetOpenThreadsForAdminAsync();
        Task MarkAsReadAsync(Guid postSaleRequestId, bool byAdmin, bool isSellerChannel);
        Task<int> GetUnreadCountAsync(Guid postSaleRequestId, bool isSellerChannel, bool byAdmin);
    }
}